import torch
import argparse
import matplotlib.pyplot as plt
from pathlib import Path

from .actor import Actor
from .critic import Critic
from .ppo import PPO
from .unity_env import UnityEnv


def parse_args():
    
    # Egitim adimini ve rollout uzunlugunu komut satirindan al.
    parser = argparse.ArgumentParser(description="Unity ML-Agent için özel PPO eğitimi")
    parser.add_argument("--steps", type=int, default=6_000)
    parser.add_argument("--rollout-steps", type=int, default=512)
    parser.add_argument("--resume", action="store_true", help="Son kayıttan eğitime devam et")
    args = parser.parse_args()
    
    # Sifir rollout adimi egitim dongusunu ilerletmeyecegi icin engelle.
    if args.steps < 1 or args.rollout_steps < 1:
        parser.error("steps ve rollout-steps pozitif olmalı")
    return args


def save_models(ppo, model_dir, steps, episodes, plot_data):
    
    # Modeller klasorunu ilk kayit sirasinda olustur.
    model_dir.mkdir(parents=True, exist_ok=True)
    
    # Actor ve critic agirliklarini ayri dosyalara yaz.
    torch.save(ppo.actor.state_dict(), model_dir / "actor.pt")
    torch.save(ppo.critic.state_dict(), model_dir / "critic.pt")
    
    # Optimizer ve adim sayisini da kaydet.
    torch.save({"actor": ppo.actor.state_dict(), "critic": ppo.critic.state_dict(),
                "optimizer": ppo.optimizer.state_dict(), "steps": steps,
                "episodes": episodes, "plot_data": plot_data},
               model_dir / "checkpoint.pt")


def show_reward_plot(plot_data, model_dir): # Reward grafik takibi.
    
    plt.figure(figsize=(8, 4))
    plt.plot(plot_data["steps"], plot_data["scores"], color="red", label="Anlik puan")
    
    plt.scatter(plot_data["end_steps"], plot_data["end_scores"],
                color="darkgoldenrod", s=30, label="Bolum sonu")
    plt.xlabel("Egitim adimi")
    plt.ylabel("Bolum puani")
    plt.title("Egitim ilerlemesi")
    plt.legend()
    plt.tight_layout()

    plt.savefig(model_dir / "reward_curve.png")
    if "inline" in plt.get_backend().lower():
        plt.show()
    plt.close()


def train(args):
    
    # Aglarin tanimini burada kur; PPO yalnizca kendisine verilen modelleri egitir.
    ppo = PPO(Actor(), Critic())
    steps = episodes = 0
    model_dir = Path(__file__).parent / "models"
    model_dir.mkdir(parents=True, exist_ok=True)
    checkpoint_path = model_dir / "checkpoint.pt"
    plot_data = {"steps": [], "scores": [], "end_steps": [], "end_scores": []}
    
    # --resume ile egitime devam etme.
    if args.resume:
        if not checkpoint_path.exists():
            raise FileNotFoundError(f"Kayıt bulunamadı: {checkpoint_path}")
        checkpoint = torch.load(checkpoint_path, map_location=ppo.device)
        ppo.actor.load_state_dict(checkpoint["actor"])
        ppo.critic.load_state_dict(checkpoint["critic"])
        ppo.optimizer.load_state_dict(checkpoint["optimizer"])
        steps, episodes = checkpoint["steps"], checkpoint["episodes"]
        plot_data = checkpoint.get("plot_data", plot_data)

    if steps >= args.steps:
        print(f"Kayit {steps} adimda. Devam etmek icin --steps degerini artir.")
        return

    # Unity Play moduna baglan.
    env = UnityEnv()
    try:
        observation = env.reset()
        episode_reward = 0.0
        updates = 0
        
        # Toplam hedef adim sayisina kadar rollout topla ve PPO guncelle.
        while steps < args.steps:
            for _ in range(min(args.rollout_steps, args.steps - steps)):
                # Actor hareket secer, unity odul ve yeni gozlemi dondurur.
                state = torch.as_tensor(observation, dtype=torch.float32, device=ppo.device)
                with torch.no_grad():
                    action_tensor, log_prob_tensor = ppo.actor.get_action(state)
                action = action_tensor.cpu().tolist()
                log_prob = float(log_prob_tensor.item())
                next_observation, reward, terminated, done = env.step(action)
                # Bu gecisi buffer'a ekle. PPO rollout sonunda kullanir.
                ppo.remember(observation, action, log_prob, reward,
                             next_observation, terminated, done)
                steps += 1
                episode_reward += reward
                plot_data["steps"].append(steps)
                plot_data["scores"].append(episode_reward)
                observation = next_observation
                # Episode bittiyse odulu yazdir ve yeni episode baslat.
                if done:
                    episodes += 1
                    print(f"Bölüm {episodes}: ödül={episode_reward:.3f}, adım={steps}", flush=True)
                    plot_data["end_steps"].append(steps)
                    plot_data["end_scores"].append(episode_reward)
                    
                    # Sonraki bolumun cizgisi ayri baslasin.
                    plot_data["steps"].append(steps)
                    plot_data["scores"].append(float("nan"))
                    episode_reward = 0.0
                    observation = env.reset()
                    
            # Buffer'daki hareketlerle actor ve critic agirliklarini guncelle.
            losses = ppo.update()
            updates += 1
            print(f"Güncelleme {updates}: adım={steps}, "
                  f"actor={losses['policy_loss']:.4f}, critic={losses['value_loss']:.4f}",
                  flush=True)
            
            # Her guncellemeden sonra en son puanlari grafige ekle.
            show_reward_plot(plot_data, model_dir)
            
            # Belirlenen araliklarda ve son adimda modelleri diske kaydet.
            if updates % 2 == 0 or steps == args.steps:
                save_models(ppo, model_dir, steps, episodes, plot_data)
    finally:
        env.close()


if __name__ == "__main__":
    train(parse_args())
