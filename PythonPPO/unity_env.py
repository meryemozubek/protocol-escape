import numpy as np
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel


class UnityEnv:
    def __init__(self):
        # Unity'nin resmi hiz ayari ornegi:
        # https://unity-technologies.github.io/ml-agents/Python-LLAPI/#engineconfigurationchannel
        config = EngineConfigurationChannel()
        self._env = UnityEnvironment(file_name=None, side_channels=[config])
        config.set_configuration_parameters(time_scale=5.0)
        self.behavior_name = None
        self.agent_id = None

    def _observation(self, step):
        # Ajanin gordugu bilgileri tek dizi yap.
        return np.concatenate([part.reshape(-1) for part in step.obs]).astype(np.float32)

    def reset(self):
        # Oyunu bastan baslat ve ajanin ilk gozlemini al.
        self._env.reset()
        self.behavior_name = list(self._env.behavior_specs)[0]
        decisions, _ = self._env.get_steps(self.behavior_name)
        self.agent_id = int(decisions.agent_id[0])
        return self._observation(decisions[self.agent_id])

    def step(self, action):
        # X ve Y hareketini oyuna gonder.
        continuous = np.asarray(action, dtype=np.float32).reshape(1, -1)
        self._env.set_actions(self.behavior_name, ActionTuple(continuous=continuous))
        self._env.step()

        # Oyundan odulu ve yeni durumu al.
        decisions, terminals = self._env.get_steps(self.behavior_name)
        # Bolum bittiyse neden bittigini kontrol et.
        if self.agent_id in terminals.agent_id_to_index:
            result = terminals[self.agent_id]
            self.agent_id = None
            return self._observation(result), float(result.reward), not result.interrupted, True
        # Bolum suruyorsa yeni durumu dondur.
        if self.agent_id in decisions.agent_id_to_index:
            result = decisions[self.agent_id]
            return self._observation(result), float(result.reward), False, False
        raise RuntimeError("Ajanın sonraki kararı veya bölüm sonu Unity'den gelmedi")

    def close(self):
        # Oyunla baglantiyi kapat.
        self._env.close()
