import argparse
from datetime import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import numpy as np
import torch

# Proje yolunu bu dosyadan bul; komutun acildigi klasore bagli kalma.
ROOT = Path(__file__).resolve().parent.parent
os.environ.setdefault('MPLCONFIGDIR', str(ROOT / '.matplotlib-cache'))
from .actor import Actor
from .critic import Critic
from .ppo import PPO
from .unity_env import UnityEnv
from .records import (SCHEMA_VERSION, append_csv, atomic_torch_save, environment_signature,
    episode_row, load_checkpoint, make_metadata, restore_rng, rng_state, seed_everything,
    show_reward_plot, summarize, write_json)


# Adim sayisi, kayittan devam ve degerlendirme seceneklerini oku.
def parse_args(argv=None):

    # Egitim adimini ve rollout uzunlugunu komut satirindan al.
    parser = argparse.ArgumentParser(description='Protocol Escape - button_task_v2 ozel PPO')
    
    # Devam ederken de toplam hedef adim sayisi kullanilir; eklenecek adim sayisi degil.
    parser.add_argument('--steps', type=int, default=200_000)
    parser.add_argument('--rollout-steps', type=int, default=1024)
    parser.add_argument('--update-epochs', type=int, default=5)
    parser.add_argument('--resume', action='store_true')
    parser.add_argument('--checkpoint', type=Path)
    parser.add_argument('--output-dir', type=Path, default=ROOT / 'PythonPPO/runs')
    parser.add_argument('--run-name', default='button_task_v2')
    parser.add_argument('--unity-project', type=Path, default=ROOT / 'YazLab1')
    parser.add_argument('--seed', type=int, default=0)
    parser.add_argument('--torch-threads', type=int, default=min(4, os.cpu_count() or 1))
    parser.add_argument('--time-scale', type=float, default=20.0)
    parser.add_argument('--timeout-wait', type=int, default=120)
    parser.add_argument('--plot-every', type=int, default=10)
    parser.add_argument('--no-plot', action='store_true')
    parser.add_argument('--evaluate', action='store_true')
    parser.add_argument('--eval-episodes', type=int, default=20)
    parser.add_argument('--sample-actions', action='store_true')
    parser.add_argument('--wait-min', type=float)
    parser.add_argument('--wait-max', type=float)
    args = parser.parse_args(argv)
    
    # Sifir rollout adimi egitim dongusunu ilerletmeyecegi icin engelle.
    if min(args.steps, args.rollout_steps, args.update_epochs, args.torch_threads,
           args.plot_every, args.eval_episodes, args.timeout_wait) < 1 or args.time_scale <= 0:
        parser.error('Adim, epoch, sure, hiz ve sayaclar pozitif olmali.')
    if (args.wait_min is None) != (args.wait_max is None):
        parser.error('--wait-min ve --wait-max birlikte verilmeli.')
    if args.wait_min is not None and (args.wait_min < 0 or args.wait_max < args.wait_min):
        parser.error('Bekleme araligi gecersiz.')
    if args.checkpoint is not None and not (args.resume or args.evaluate):
        parser.error('--checkpoint icin --resume veya --evaluate sec.')
    if args.resume and args.evaluate:
        parser.error('--resume ve --evaluate birlikte kullanilmaz.')
    return args


# Acikca dosya verilmediyse en son kaydin yolunu latest.json icinden al.
def checkpoint_path(args):
    if args.checkpoint is not None:
        return args.checkpoint.resolve()
    latest = args.output_dir / 'latest.json'
    if not latest.exists():
        raise FileNotFoundError('Yeni profile ait kayit yok. Once --resume olmadan sifirdan egit.')
    return Path(json.loads(latest.read_text(encoding='utf-8'))['checkpoint'])


# Her calistirmaya ayri klasor ac; onceki egitimin sonuclari ezilmesin.
def new_run_directory(args, prefix=None):
    label = re.sub(r'[^A-Za-z0-9_-]+', '_', prefix or args.run_name).strip('_') or 'button_task'
    target = args.output_dir.resolve() / (datetime.now().strftime('%Y%m%d_%H%M%S_%f') + '_' + label)
    target.mkdir(parents=True, exist_ok=False)
    return target


# Agirliklari ayri kaydet; devam icin optimizer ve gecmisi de checkpoint icine koy.
def save_models(ppo, model_dir, steps, episodes, plot_data, metadata, rows, updates):
    # Actor ve critic agirliklarini ayri dosyalara yaz.
    atomic_torch_save(ppo.actor.state_dict(), model_dir / 'actor.pt')
    atomic_torch_save(ppo.critic.state_dict(), model_dir / 'critic.pt')
    # Optimizer ve adim sayisini da kaydet.
    atomic_torch_save({'schema_version': SCHEMA_VERSION,
        'actor': ppo.actor.state_dict(), 'critic': ppo.critic.state_dict(),
        'optimizer': ppo.optimizer.state_dict(), 'steps': steps, 'episodes': episodes,
        'updates': updates, 'plot_data': plot_data, 'episode_rows': rows,
        'metadata': metadata, 'rng_state': rng_state()}, model_dir / 'checkpoint.pt')
    # Varsayilan devam ve degerlendirme bu kaydi bulsun.
    write_json(model_dir.parent / 'latest.json', {'run_dir': str(model_dir),
        'checkpoint': str(model_dir / 'checkpoint.pt'), 'steps': steps, 'profile': 'button_task_v2'})


# Baglanti hizini, bekleme suresini ve varsa buton surelerini Unity ortamina ilet.
def open_environment(args):
    wait_range = None if args.wait_min is None else (args.wait_min, args.wait_max)
    return UnityEnv(seed=args.seed, time_scale=args.time_scale,
                    timeout_wait=args.timeout_wait, wait_range=wait_range)


# Unityden veri topla, PPO ile ogren ve duzenli araliklarla kaydet.
def train(args):
    
    # Eski Namespace cagiran araclar icin eksik CLI alanlarini varsayilanlarla tamamla.
    for key, value in vars(parse_args([])).items():
        if not hasattr(args, key): setattr(args, key, value)
    seed_everything(args.seed, args.torch_threads)
    
    # Resume secilmediyse sifirdan basla; eski agirliklari kendiliginden yukleme.
    restored = load_checkpoint(checkpoint_path(args)) if args.resume else None
    if restored is not None and restored['steps'] >= args.steps:
        print(f"Kayit {restored['steps']} adimda. --steps toplam hedefi daha buyuk olmali.")
        return None
    run_dir = new_run_directory(args)
    write_json(run_dir / 'status.json', {'status': 'connecting', 'target_steps': args.steps})
    print(f'PPO: hedef={args.steps}, rollout={args.rollout_steps}, epoch={args.update_epochs}, seed={args.seed}', flush=True)
    print(f'Kayit klasoru: {run_dir}', flush=True)
    env = None; ppo = None; metadata = None
    steps = episodes = updates = 0
    rows = []
    plot_data = {'steps': [], 'scores': [], 'end_steps': [], 'end_scores': []}
    status = 'failed'
    try:
        # Unity Play moduna baglan.
        env = open_environment(args)
        observation = env.reset()
        metadata = make_metadata(args, env)
        # Aglarin tanimini burada kur; PPO yalnizca kendisine verilen modelleri egitir.
        ppo = PPO(Actor(env.observation_size, env.action_size), Critic(env.observation_size), args.update_epochs)
        # --resume ile egitime devam etme.
        if restored is not None:
            # Ayni modeli farkli sahne veya odul kurallariyla yanlislikla devam ettirme.
            if restored['metadata']['environment_signature'] != environment_signature(env.environment_info):
                raise ValueError('Checkpoint ile Unity sahne/odul/bekleme profili farkli. Yeni kosuyu sifirdan baslat.')
            ppo.actor.load_state_dict(restored['actor'])
            ppo.critic.load_state_dict(restored['critic'])
            ppo.optimizer.load_state_dict(restored['optimizer'])
            steps, episodes, updates = restored['steps'], restored['episodes'], restored['updates']
            rows = restored['episode_rows']
            plot_data = restored['plot_data']
            metadata['resume_from'] = str(checkpoint_path(args))
            metadata['resume_sha256'] = hashlib.sha256(checkpoint_path(args).read_bytes()).hexdigest()
            for row in rows: append_csv(run_dir / 'episodes.csv', row)
            restore_rng(restored.get('rng_state'))
        write_json(run_dir / 'experiment.json', metadata)
        episode_reward = 0.0; episode_decisions = 0
        status = 'running'
        
        # Toplam hedef adim sayisina kadar rollout topla ve PPO guncelle.
        while steps < args.steps:
            
            # Son rollout daha kisa olabilir; istenen toplam adimi asma.
            for _ in range(min(args.rollout_steps, args.steps - steps)):
                
                # Actor hareket secer, unity odul ve yeni gozlemi dondurur.
                state = torch.as_tensor(observation, dtype=torch.float32, device=ppo.device)
                with torch.no_grad():
                    action_tensor, log_prob_tensor = ppo.actor.get_action(state)
                action = action_tensor.cpu().numpy()
                
                # Hareketi oyuna gonder; gorev bitisi ile bolum bitisini ayri al.
                next_observation, reward, terminated, done = env.step(action)
                
                # Bu gecisi buffer'a ekle. PPO rollout sonunda kullanir.
                ppo.remember(observation, action, float(log_prob_tensor), reward,
                             next_observation, terminated, done)
                steps += 1; episode_decisions += 1; episode_reward += reward
                plot_data['steps'].append(steps); plot_data['scores'].append(episode_reward)
                observation = next_observation
                
                # Episode bittiyse odulu yazdir ve yeni episode baslat.
                if done:
                    observation = env.next_episode()
                    episodes += 1
                    row = episode_row(env.last_episode_info, episodes, steps, episode_decisions, episode_reward)
                    rows.append(row); append_csv(run_dir / 'episodes.csv', row)
                    print(f"Bolum {episodes}: odul={episode_reward:.4f}, buton={row['completed_buttons']}/3, "
                          f"neden={row['termination_reason']}, karar={episode_decisions}, adim={steps}", flush=True)
                    plot_data['end_steps'].append(steps); plot_data['end_scores'].append(episode_reward)
                    
                    # Sonraki bolumun cizgisi ayri baslasin.
                    plot_data['steps'].append(steps); plot_data['scores'].append(float('nan'))
                    episode_reward = 0.0; episode_decisions = 0
                    
            # Buffer'daki hareketlerle actor ve critic agirliklarini guncelle.
            losses = ppo.update(); updates += 1
            append_csv(run_dir / 'updates.csv', {'update': updates, 'steps': steps, **losses})
            print(f"Guncelleme {updates}: adim={steps}, actor={losses['policy_loss']:.4f}, "
                  f"critic={losses['value_loss']:.4f}, KL={losses['approx_kl']:.5f}, "
                  f"clip={losses['clip_fraction']:.3f}", flush=True)
            if not args.no_plot and (updates % args.plot_every == 0 or steps == args.steps):
                show_reward_plot(plot_data, rows, run_dir)
                
            # Belirlenen araliklarda ve son adimda modelleri diske kaydet.
            if updates % 2 == 0 or steps == args.steps:
                save_models(ppo, run_dir, steps, episodes, plot_data, metadata, rows, updates)
            write_json(run_dir / 'summary.json', {**summarize(rows), 'steps': steps, 'status': status})
            write_json(run_dir / 'status.json', {'status': status, 'steps': steps, 'episodes': episodes})
        status = 'completed'
    except KeyboardInterrupt:
        status = 'interrupted'
        print('Egitim kullanici tarafindan durduruldu; mevcut kayit saklaniyor.', flush=True)
    except Exception as exc:
        status = 'failed'
        write_json(run_dir / 'error.json', {'error': str(exc), 'steps': steps})
        raise
    finally:
        try:
            # Egitim yarida kesilse de eldeki agirliklari ve ilerlemeyi kaydet.
            if ppo is not None and metadata is not None and steps > 0:
                save_models(ppo, run_dir, steps, episodes, plot_data, metadata, rows, updates)
                if not args.no_plot: show_reward_plot(plot_data, rows, run_dir)
                write_json(run_dir / 'summary.json', {**summarize(rows), 'steps': steps, 'status': status})
            write_json(run_dir / 'status.json', {'status': status, 'steps': steps, 'episodes': episodes})
        finally:
            if env is not None: env.close()
    print(f'Egitim durumu: {status}. Sonuclar: {run_dir}', flush=True)
    return run_dir


# Kayitli actor ile oynamayi dene; burada optimizer calismaz, model ogrenmez.
def evaluate(args):
    seed_everything(args.seed, args.torch_threads)
    source = checkpoint_path(args)
    checkpoint = load_checkpoint(source)
    run_dir = new_run_directory(args, 'evaluation')
    env = None; rows = []
    try:
        env = open_environment(args)
        observation = env.reset()
        device = torch.device('cuda' if torch.cuda.is_available() else
                              'mps' if torch.backends.mps.is_available() else 'cpu')
        actor = Actor(env.observation_size, env.action_size).to(device)
        actor.load_state_dict(checkpoint['actor']); actor.eval()
        write_json(run_dir / 'experiment.json', {**make_metadata(args, env),
            'mode': 'evaluation', 'checkpoint': str(source),
            'checkpoint_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
            'deterministic': not args.sample_actions})
        steps = episode_decisions = 0; reward_sum = 0.0
        while len(rows) < args.eval_episodes:
            with torch.no_grad():
                action, _ = actor.get_action(torch.as_tensor(observation, dtype=torch.float32, device=device),
                                             
                                             # Normalde ortalama hareketi sec; sample-actions verilirse rastgele ornekle.
                                             deterministic=not args.sample_actions)
            observation, reward, _, done = env.step(action.cpu().numpy())
            steps += 1; episode_decisions += 1; reward_sum += reward
            if done:
                observation = env.next_episode()
                row = episode_row(env.last_episode_info, len(rows)+1, steps, episode_decisions, reward_sum)
                rows.append(row); append_csv(run_dir / 'episodes.csv', row)
                print(f"Degerlendirme {len(rows)}/{args.eval_episodes}: "
                      f"buton={row['completed_buttons']}/3, odul={reward_sum:.4f}", flush=True)
                episode_decisions = 0; reward_sum = 0.0
        result = summarize(rows)
        write_json(run_dir / 'summary.json', result)
        print(json.dumps(result, indent=2), flush=True)
        print(f'Degerlendirme kaydi: {run_dir}', flush=True)
        return run_dir
    finally:
        if env is not None: env.close()


# Komuttaki secime gore egitim veya degerlendirme akisini baslat.
def main():
    args = parse_args()
    return evaluate(args) if args.evaluate else train(args)


if __name__ == '__main__':
    main()
