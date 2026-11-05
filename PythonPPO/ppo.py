import torch
from torch import nn
from torch.distributions import Normal, TanhTransform, TransformedDistribution
from .buffer import RolloutBuffer


# Ajanin beklenenden iyi mi kotu mu yaptigini hesapla.
def compute_gae(rewards, values, next_values, terminated, done, gamma=0.999, gae_lambda=0.95):
    
    # Gorev bittiyse sonraki degeri sayma; sadece sure bittiyse tahmini kullan.
    deltas = rewards + gamma * (1 - terminated) * next_values - values
    advantages = torch.zeros_like(rewards)
    following = torch.zeros((), device=rewards.device)
    
    # Sonraki odulleri onceki hareketlere de paylastir.
    for index in range(len(rewards) - 1, -1, -1):
        following = deltas[index] + gamma * gae_lambda * (1 - done[index]) * following
        advantages[index] = following
        
    # Critic hedefini avantaj ile onceki deger tahminini toplayarak bul.
    return advantages, advantages + values


class PPO:
    def __init__(self, actor, critic, update_epochs=5):
        # nvdia -> macbook -> islemci
        self.device = torch.device('cuda' if torch.cuda.is_available() else
                                   'mps' if torch.backends.mps.is_available() else 'cpu')
        self.actor = actor.to(self.device)
        self.critic = critic.to(self.device)
        
        # Iki modeli birlikte egitmek icin optimizer kur.
        self.parameters = list(actor.parameters()) + list(critic.parameters())
        self.optimizer = torch.optim.Adam(self.parameters, lr=3e-4)
        
        # Gelecekteki odullarin onemi.
        self.gamma = 0.999
        
        # Avantaji hesaplarken sonraki adimlarin etkisini belirle.
        self.gae_lambda = 0.95
        
        # Yeni hareket olasiliginin eskisinden cok uzaklasmasini sinirla.
        self.clip_epsilon = 0.2
        
        # Ayni veriye kac kez bakilacak?
        self.update_epochs = update_epochs
        self.buffer = RolloutBuffer()

    # Hareketi ve aldigi odulu kaydet.
    def remember(self, observation, action, log_prob, reward, next_observation, terminated, done):
        
        # Verileri kopyalayip sakla.
        self.buffer.add(torch.as_tensor(observation, dtype=torch.float32).clone(),
                        torch.as_tensor(action, dtype=torch.float32).clone(), float(log_prob),
                        float(reward), torch.as_tensor(next_observation, dtype=torch.float32).clone(),
                        bool(terminated), bool(done))

    def update(self):
        if not self.buffer.rewards:
            raise RuntimeError('Bos rollout ile PPO guncellenemez.')
            
        # Kaydedilen bilgileri egitimde kullanilacak hale getir.
        states = torch.stack(self.buffer.observations).to(self.device)
        actions = torch.stack(self.buffer.actions).to(self.device)
        old_logs = torch.tensor(self.buffer.log_probs, dtype=torch.float32, device=self.device)
        rewards = torch.tensor(self.buffer.rewards, dtype=torch.float32, device=self.device)
        next_states = torch.stack(self.buffer.next_observations).to(self.device)
        terminated = torch.tensor(self.buffer.terminated, dtype=torch.float32, device=self.device)
        done = torch.tensor(self.buffer.done, dtype=torch.float32, device=self.device)
        
        # Veride NaN veya sonsuz deger varsa agirliklari bozmadan dur.
        if not all(torch.isfinite(t).all().item() for t in [states, actions, old_logs, rewards, next_states]):
            raise RuntimeError('Rollout icinde NaN/Infinity bulundu.')
        with torch.no_grad():
            
            # Avantaj ve hedef hesabi icin gradyan tutmaya gerek yok.
            values = self.critic(states)
            advantages, returns = compute_gae(rewards, values, self.critic(next_states),
                                             terminated, done, self.gamma, self.gae_lambda)
            # Sayilari dengeli hale getir.
            advantages = (advantages - advantages.mean()) / (advantages.std(unbiased=False) + 1e-8)
        records = []
        
        # Her epoch boyunca rollout verisinin tamamiyla egit.
        for _ in range(self.update_epochs):
            mean, std = self.actor(states)
            normal = Normal(mean, std)
            distribution = TransformedDistribution(normal, [TanhTransform(cache_size=1)])
            new_logs = distribution.log_prob(actions).sum(dim=-1)
            
            # Bu hareketin eski ve yeni olasiligini karsilastir.
            log_ratio = new_logs - old_logs
            ratio = log_ratio.exp()
            policy_loss = -torch.minimum(ratio * advantages,
                ratio.clamp(1 - self.clip_epsilon, 1 + self.clip_epsilon) * advantages).mean()
            
            # Critic'in tahmin hatasini hesapla.
            value_loss = (self.critic(states) - returns).square().mean()
            loss = policy_loss + 0.5 * value_loss
            if not torch.isfinite(loss).item():
                raise RuntimeError('PPO kaybi sonlu degil; agirliklar guncellenmedi.')
                
            # Actor ve critic'i guncelle.
            self.optimizer.zero_grad()
            loss.backward()
            
            # Cok buyuk gradyanlari kirp; kararsiz bir guncellemeyi onle.
            grad_norm = nn.utils.clip_grad_norm_(self.parameters, 0.5, error_if_nonfinite=True)
            self.optimizer.step()
            with torch.no_grad():
                
                # Kayip ve olasilik degisimlerini kaydet; egitim gidisati buradan izlenir.
                records.append({
                    'policy_loss': float(policy_loss), 'value_loss': float(value_loss),
                    'approx_kl': float(((ratio - 1) - log_ratio).mean()),
                    'clip_fraction': float(((ratio - 1).abs() > self.clip_epsilon).float().mean()),
                    'pre_tanh_entropy': float(normal.entropy().sum(dim=-1).mean()),
                    'policy_std': float(std.mean()), 'grad_norm': float(grad_norm),
                })
                
        # Epoch sonu olcumlerinin ortalamasini al.
        result = {name: sum(row[name] for row in records) / len(records) for name in records[0]}
        result['action_mean'] = float(actions.mean())
        result['action_std'] = float(actions.std(unbiased=False))
        # Sonraki egitim icin buffer'i bosalt.
        self.buffer.clear()
        return result
