import numpy as np
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.exception import UnityTimeOutException, UnityWorkerInUseException
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from .metrics_channel import TrainingMetricsChannel


# Kurulum yarida kesilirse bile haberlesme kaynagini kapat.
class ManagedUnityEnvironment(UnityEnvironment):
    # Kesilen baglantinin portunu temizle.
    def __init__(self, *args, **kwargs):
        try:
            super().__init__(*args, **kwargs)
        except BaseException:
            self._close(timeout=0)
            raise

    def _close(self, timeout=None):
        if getattr(self, '_communicator', None) is not None:
            super()._close(timeout=0 if timeout is None else timeout)
        else:
            self._loaded = False

    def close(self):
        self._close(timeout=0)


# Python hareketlerini Unityye, Unity gozlem ve odullerini Pythona aktar.
class UnityEnv:
    def __init__(self, seed=0, time_scale=20.0, timeout_wait=120, wait_range=None): # Meryem'in bilgisayari ile.
        # Bolum sonu bilgileri icin ozel veri kanalini hazirla.
        self.metrics = TrainingMetricsChannel()
        # Unity'nin resmi hiz ayari ornegi:
        # https://unity-technologies.github.io/ml-agents/Python-LLAPI/#engineconfigurationchannel
        config = EngineConfigurationChannel()
        self.behavior_name = None
        self.agent_id = None
        self.last_episode_info = None
        self.observation_size = None
        self.action_size = None
        self.observation_parts = None
        self.observation_specs = None
        print(f'Unity bekleniyor: port 5004, en fazla {timeout_wait} saniye.\n'
              'YazLab1 > AgentTraining > Play (Behavior Type: Default).', flush=True)
        try:
            # File name yoksa bir oyun dosyasi baslatma; acik Unity Editor baglantisini bekle.
            self._env = ManagedUnityEnvironment(file_name=None, seed=seed,
                side_channels=[config, self.metrics], timeout_wait=timeout_wait)
        except UnityWorkerInUseException as exc:
            raise RuntimeError('5004 portu kullanimda. Onceki egitimin kernelini kapat veya yeniden baslat.') from exc
        except UnityTimeOutException as exc:
            raise RuntimeError('Unity baglanamadi. Dogru projede Play acik ve Pause kapali olmali.') from exc
        config.set_configuration_parameters(time_scale=time_scale)
        if wait_range is not None:
            self.metrics.configure_wait_range(*wait_range)
        print(f'Unity baglantisi kuruldu. Hiz talebi: {time_scale}x.', flush=True)

    @property
    def environment_info(self):
        return self.metrics.environment_info

    # Ajanin gordugu bilgileri tek dizi yap.
    def _observation(self, step):
        self.observation_parts = [part.copy() for part in step.obs]
        observation = np.concatenate([part.reshape(-1) for part in step.obs]).astype(np.float32)
        if observation.shape != (self.observation_size,) or not np.isfinite(observation).all():
            raise RuntimeError('Unity gozlem boyutu veya sayisal degerleri gecersiz.')
        return observation

    # Tek ajanin kimligini al. Sonraki hareket ve sonucu bu kimlikle eslestir.
    def _adopt_decision(self, decisions):
        if len(decisions.agent_id) != 1:
            raise RuntimeError('Bu egitim tek ajan gerektirir; birden fazla karar alindi.')
        self.agent_id = int(decisions.agent_id[0])
        return self._observation(decisions[self.agent_id])

    # Unityyi bir adim ilerlet; yanit kesilirse anlasilir hata ver.
    def _advance(self):
        try:
            self._env.step()
        except UnityTimeOutException as exc:
            raise RuntimeError('Unity egitim sirasinda yanit vermedi. Play acik, Pause ve Error Pause kapali olmali.') from exc

    # Ilk baglantida ortami sifirla ve gercek gozlem/aksiyon boyutlarini kontrol et.
    def reset(self):
        # Oyunu bastan baslat ve ajanin ilk gozlemini al.
        self._env.reset()
        behaviors = list(self._env.behavior_specs)
        if len(behaviors) != 1:
            raise RuntimeError(f'Tek behavior bekleniyordu; alinan: {behaviors}')
        self.behavior_name = behaviors[0]
        spec = self._env.behavior_specs[self.behavior_name]
        self.observation_specs = [
            {'name': getattr(part, 'name', f'sensor_{index}'), 'shape': list(part.shape)}
            for index, part in enumerate(spec.observation_specs)]
        self.observation_size = sum(int(np.prod(part.shape)) for part in spec.observation_specs)
        self.action_size = spec.action_spec.continuous_size
        print(f'Gercek gozlem: {self.observation_size}; sensorler={self.observation_specs}; '
              f'surekli aksiyon={self.action_size}', flush=True)
        
        # Bu gorev 4 vektor + 27 isin gozlemi ve X/Y icin 2 surekli aksiyon bekliyor.
        if self.observation_size != 31 or self.action_size != 2 or spec.action_spec.discrete_size != 0:
            raise RuntimeError('button_task_v2 icin 4 vektor + 27 ray = 31 gozlem ve 2 surekli aksiyon gerekiyor. '
                               'Unity sahnesini ve derlemeyi kontrol et; eski 44 girisli modeli kullanma.')
        if self.environment_info is None or self.environment_info.get('profile') != 'button_task_v2':
            raise RuntimeError('Yeni Unity telemetri kanali bulunamadi. Guncel kodlar derlendikten sonra Play baslat.')
        info = self.environment_info
        print(f"Profil={info['profile']}; MaxStep={info['max_step']}; "
              f"bolum={info['max_step'] * info['fixed_delta_time']:.1f} simulasyon saniyesi.", flush=True)
        self.metrics.episode_summaries.clear()
        self.last_episode_info = None
        decisions, _ = self._env.get_steps(self.behavior_name)
        return self._adopt_decision(decisions)

    # X ve Y hareketini oyuna gonder.
    def step(self, action):
        if self.agent_id is None:
            raise RuntimeError('Bitmis bolumden sonra next_episode() cagirilmali.')
        continuous = np.asarray(action, dtype=np.float32).reshape(1, -1)
        if continuous.shape != (1, self.action_size) or not np.isfinite(continuous).all():
            raise ValueError('Aksiyon sekli/degeri gecersiz.')
        self._env.set_actions(self.behavior_name, ActionTuple(continuous=continuous))
        self._advance()
        
        # Oyundan odulu ve yeni durumu al.
        decisions, terminals = self._env.get_steps(self.behavior_name)
        
        # Bolum bittiyse neden bittigini kontrol et.
        if self.agent_id in terminals.agent_id_to_index:
            result = terminals[self.agent_id]
            self.agent_id = None
            self.last_episode_info = self.metrics.pop_summary()
            
            # Sure dolmasi terminated degil; GAE sonraki durum tahminini kullanabilir.
            return self._observation(result), float(result.reward), not result.interrupted, True
        
        # Bolum suruyorsa yeni durumu dondur.
        if self.agent_id in decisions.agent_id_to_index:
            result = decisions[self.agent_id]
            return self._observation(result), float(result.reward), False, False
        raise RuntimeError('Ajanin sonraki karari veya terminal gozlemi gelmedi.')

    # Unitynin otomatik baslattigi yeni bolumun kararini ve onceki bolumun ozetini bekle.
    def next_episode(self):
        
        # Unity zaten otomatik resetledi. Yeni env.reset() ile ikinci kez sifirlama.
        if self.agent_id is not None:
            raise RuntimeError('Mevcut bolum henuz bitmedi.')
        for _ in range(100):
            decisions, _ = self._env.get_steps(self.behavior_name)
            if len(decisions.agent_id):
                observation = self._adopt_decision(decisions)
                if self.last_episode_info is None:
                    self.last_episode_info = self.metrics.pop_summary()
                if self.last_episode_info is None:
                    raise RuntimeError('Bolum sonu telemetrisi eksik; bu bolum icin basari uydurulmadi.')
                return observation
            self._advance()
        raise RuntimeError('Yeni bolum karari gelmedi.')

    def close(self):
        # Oyunla baglantiyi kapat.
        self._env.close()
