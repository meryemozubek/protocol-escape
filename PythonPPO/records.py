import csv
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import random
import tempfile
from datetime import datetime
import numpy as np
import torch

# Kayit biciminin surumu; eski modeller yanlislikla yuklenmesin.
SCHEMA_VERSION = 2


# Klasoru hazirla ve bilgileri okunabilir JSON olarak kaydet.
def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False), encoding='utf-8')


# Sonucu tablonun sonuna ekle; sutun basliklarini sadece ilk yazista olustur.
def append_csv(path, row):
    path = Path(path)
    exists = path.exists() and path.stat().st_size > 0
    with path.open('a', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(row))
        if not exists:
            writer.writeheader()
        writer.writerow(row)


# Once gecici dosyaya yaz; kayit tamamlaninca asil dosyanin yerine koy.
def atomic_torch_save(value, path):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(mode='wb', dir=path.parent, suffix='.tmp', delete=False) as stream:
        temporary = Path(stream.name)
        try:
            torch.save(value, stream)
            stream.flush()
            # Dosyayi degistirmeden once yazilan verinin diske aktarilmasini iste.
            os.fsync(stream.fileno())
        except BaseException:
            stream.close()
            temporary.unlink(missing_ok=True)
            raise
    try:
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)


# Kaydi islemciye yukle; yeni egitim duzenine uygun oldugunu kontrol et.
def load_checkpoint(path):
    with Path(path).open('rb') as stream:
        value = torch.load(stream, map_location='cpu')
    if value.get('schema_version') != SCHEMA_VERSION or value.get('metadata', {}).get('observation_size') != 31:
        raise ValueError('Bu checkpoint eski/uyumsuz (44 girisli olabilir). button_task_v2 icin sifirdan egit; --resume kullanma.')
    return value


# Sahne ve odul ayarlarindan bir imza uret; devam ederken ortam degismis mi anlasilsin.
def environment_signature(info):
    fields = ['profile', 'vector_observation_size', 'expected_observation_size', 'max_step',
              'decision_period', 'fixed_delta_time', 'move_speed', 'observation_range',
              'wait_min', 'wait_max', 'button_x', 'button_y']
    value = {key: info[key] for key in fields}
    value['reward_settings'] = {key: item for key, item in info['reward_settings'].items()
                                if not key.startswith('m_')}
    return hashlib.sha256(json.dumps(value, sort_keys=True).encode('utf-8')).hexdigest()


# Dosya iceriklerinin hash degerlerini tut; egitimde hangi kodun kullanildigi izlenebilsin.
def source_snapshot(project):
    project = Path(project)
    root = Path(__file__).resolve().parent.parent
    files = {str(p): p for p in (root / 'PythonPPO').glob('*.py')}
    for relative in ['Assets/Scenes/AgentTraining.unity', 'Assets/Scripts/Agents/MLAgent.cs',
                     'Assets/Scripts/Rewards/AgentRewardConfig.cs', 'Assets/Scripts/Rewards/AgentRewardPolicy.cs',
                     'Assets/Scripts/Rewards/DefaultAgentRewardConfig.asset',
                     'Assets/Scripts/Training/TrainingPlayerPassSimulator.cs',
                     'Assets/Scripts/Training/TrainingMetricsChannel.cs', 'ProjectSettings/TimeManager.asset']:
        p = project / relative
        if p.exists(): files[str(p)] = p
    return {name: hashlib.sha256(p.read_bytes()).hexdigest() for name, p in files.items()}


# Egitim ayarlarini, ortam bilgisini ve kutuphane surumlerini bir araya getir.
def make_metadata(args, env):
    versions = {}
    for package in ['torch', 'numpy', 'mlagents-envs', 'protobuf', 'matplotlib']:
        versions[package] = importlib.metadata.version(package)
    return {'created_at': datetime.now().isoformat(timespec='seconds'), 'profile': 'button_task_v2',
            'observation_size': env.observation_size, 'action_size': env.action_size,
            'observation_specs': env.observation_specs, 'environment': env.environment_info,
            'environment_signature': environment_signature(env.environment_info),
            'arguments': {k: str(v) if isinstance(v, Path) else v for k, v in vars(args).items()},
            'hyperparameters': {'learning_rate': 0.0003, 'gamma': 0.999, 'gae_lambda': 0.95,
                'clip_epsilon': 0.2, 'update_epochs': args.update_epochs, 'value_coefficient': 0.5,
                'gradient_norm': 0.5, 'entropy_bonus': 0.0, 'minibatch': False},
            'versions': versions, 'source_sha256': source_snapshot(args.unity_project)}


# Rastgelelik baslangicini ve PyTorch islemci thread sayisini ayarla.
def seed_everything(seed, threads):
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    if torch.cuda.is_available(): torch.cuda.manual_seed_all(seed)
    torch.set_num_threads(threads)


# Kayittan devam etmek icin rastgele sayi ureticilerinin mevcut durumunu sakla.
def rng_state():
    return {'python': random.getstate(), 'numpy': np.random.get_state(),
            'torch': torch.get_rng_state(),
            'cuda': torch.cuda.get_rng_state_all() if torch.cuda.is_available() else None}


# Rastgele sayi ureticilerini kayittaki durumuna geri getir.
def restore_rng(value):
    if not value: return
    random.setstate(value['python'])
    np.random.set_state(value['numpy'])
    torch.set_rng_state(value['torch'])
    if torch.cuda.is_available() and value.get('cuda') is not None:
        torch.cuda.set_rng_state_all(value['cuda'])


# Unity sonucunu kontrol et ve bir bolum icin CSV satiri hazirla.
def episode_row(info, episode, steps, decisions, reward):
    if info is None or info.get('termination_reason') not in ['completed', 'max_step']:
        raise RuntimeError('Gecerli bolum telemetrisi bulunamadi; basari tahmin edilmedi.')
    completed = int(info['completed_buttons'])
    # Butonlar sirayla tamamlanmali; atlanmis veya birden fazla sayilmis gorevi kabul etme.
    if not 0 <= completed <= 3 or info['completion_counts'] != [int(i < completed) for i in range(3)]:
        raise RuntimeError('Buton tamamlama sayilari gorev sirasiyla tutarsiz.')
    if (info['termination_reason'] == 'completed') != (completed == 3):
        raise RuntimeError('Bolum sonu nedeni gorev sayisiyla tutarsiz.')
    row = {'episode': episode, 'total_steps': steps, 'episode_decisions': decisions,
           'reward': reward, 'simulation_seconds': info['simulation_seconds'],
           'completed_buttons': completed, 'termination_reason': info['termination_reason'],
           'unity_episode': info['unity_episode']}
    for i in range(3):
        row[f'button{i+1}_reaches'] = info['reached_counts'][i]
        row[f'button{i+1}_completed'] = info['completion_counts'][i]
        row[f'button{i+1}_wait_cancellations'] = info['wait_cancellations'][i]
        row[f'button{i+1}_minimum_distance'] = info['minimum_target_distances'][i]
    return row


# Tum bolumlerden basari orani, ortalama odul ve bitis nedenlerini hesapla.
def summarize(rows):
    if not rows: return {'episodes': 0, 'success_rate': None}
    n = len(rows)
    return {'episodes': n, 'success_rate': sum(r['completed_buttons'] == 3 for r in rows) / n,
            'button_completion_rates': [sum(r[f'button{i}_completed'] for r in rows) / n for i in [1,2,3]],
            'mean_reward': float(np.mean([r['reward'] for r in rows])),
            'last20_mean_reward': float(np.mean([r['reward'] for r in rows[-20:]])),
            'mean_simulation_seconds': float(np.mean([r['simulation_seconds'] for r in rows])),
            'termination_counts': {reason: sum(r['termination_reason'] == reason for r in rows)
                                   for reason in ['completed', 'max_step']}}


# Odul gidisatini ve tamamlanan buton sayisini iki ayri grafikte goster.
def show_reward_plot(plot_data, rows, model_dir):  # Reward grafik takibi.
    import matplotlib.pyplot as plt
    figure, axes = plt.subplots(2, 1, figsize=(9, 7))
    # Uzun egitimlerde grafigi hafifletmek icin adimlari seyrelterek ciz.
    stride = max(1, len(plot_data['steps']) // 10000)
    axes[0].plot(plot_data['steps'][::stride], plot_data['scores'][::stride],
                 color='tomato', alpha=0.5, label='Bolum ici toplam')
    axes[0].scatter(plot_data['end_steps'], plot_data['end_scores'], s=14, color='goldenrod', label='Bolum sonu')
    axes[0].set_ylabel('Odul'); axes[0].legend()
    axes[1].step([r['total_steps'] for r in rows], [r['completed_buttons'] for r in rows], where='post')
    axes[1].set_ylim(-0.1, 3.1); axes[1].set_yticks([0,1,2,3])
    axes[1].set_ylabel('Tamamlanan buton'); axes[1].set_xlabel('Python karar sayisi')
    figure.suptitle('Protocol Escape - button_task_v2')
    figure.tight_layout()
    figure.savefig(Path(model_dir) / 'reward_curve.png')
    if 'inline' in plt.get_backend().lower(): plt.show()
    plt.close(figure)
