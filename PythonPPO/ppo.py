import torch
from torch import nn
from torch.distributions import Normal, TanhTransform, TransformedDistribution

from .buffer import RolloutBuffer


class PPO:

    def __init__(self, actor, critic):
        
        # nvdia -> macbook -> islemci
        if torch.cuda.is_available():
            self.device = torch.device("cuda")
        elif torch.backends.mps.is_available():
            self.device = torch.device("mps")
        else:
            self.device = torch.device("cpu")
        self.actor = actor.to(self.device)
        self.critic = critic.to(self.device)

        # Iki modeli birlikte egitmek icin optimizer kur.
        self.parameters = list(self.actor.parameters()) + list(self.critic.parameters())
        self.optimizer = torch.optim.Adam(self.parameters, lr=3e-4)

        self.gamma = 0.99         # Gelecekteki odullarin onemi.
        
        self.gae_lambda = 0.95    # Odullerin onceki adimlara etkisi.
        
        self.clip_epsilon = 0.2   # Actor'un bir anda cok degismesini engeller.
        
        self.update_epochs = 10   # Ayni veriye kac kez bakilacak?
        
        self.batch_size = 64      # Bir seferde kac hareket kullanilacak?
        self.buffer = RolloutBuffer()

    def remember(self, observation, action, log_prob, reward, next_observation,
                 terminated, done):
        
        # Hareketi ve aldigi odulu kaydet.
        state = torch.as_tensor(observation, dtype=torch.float32)
        next_state = torch.as_tensor(next_observation, dtype=torch.float32)
        move = torch.as_tensor(action, dtype=torch.float32)

        # Verileri kopyalayip sakla.
        self.buffer.add(state.detach().clone(), move.detach().clone(), float(log_prob),
                        float(reward), next_state.detach().clone(), bool(terminated), bool(done))

    def update(self):
        
        # Kaydedilen bilgileri egitimde kullanilacak hale getir.
        states = torch.stack(self.buffer.observations).to(self.device)
        actions = torch.stack(self.buffer.actions).to(self.device)
        old_log_probs = torch.tensor(self.buffer.log_probs, device=self.device)
        rewards = torch.tensor(self.buffer.rewards, device=self.device)
        next_states = torch.stack(self.buffer.next_observations).to(self.device)
        terminated = torch.tensor(self.buffer.terminated, dtype=torch.float32, device=self.device)
        done = torch.tensor(self.buffer.done, dtype=torch.float32, device=self.device)

        # Ajanin beklenenden iyi mi kotu mu yaptigini hesapla.
        with torch.no_grad():
            values = self.critic(states)
            next_values = self.critic(next_states)
            # Gorev veya olumle bittiyse sonraki odulu sayma.
            deltas = rewards + self.gamma * (1 - terminated) * next_values - values

            # Sonraki odulleri onceki hareketlere de paylastir.
            advantages = torch.zeros_like(rewards)
            gae = 0.0
            for t in range(len(rewards) - 1, -1, -1):
                gae = deltas[t] + self.gamma * self.gae_lambda * (1 - done[t]) * gae
                advantages[t] = gae

            returns = advantages + values
            
            # Sayilari dengeli hale getir.
            advantages = (advantages - advantages.mean()) / (advantages.std(unbiased=False) + 1e-8)

        policy_losses, value_losses = [], []
        # Hareketleri karistir ve kucuk gruplarla egit.
        for _ in range(self.update_epochs):
            for indices in torch.randperm(len(rewards), device=self.device).split(self.batch_size):
                mean, std = self.actor(states[indices])
                normal = Normal(mean, std)
                distribution = TransformedDistribution(normal, [TanhTransform(cache_size=1)])
                new_log_probs = distribution.log_prob(actions[indices]).sum(dim=-1)

                # Bu hareketin eski ve yeni olasiligini karsilastir.
                ratio = torch.exp(new_log_probs - old_log_probs[indices])
                unclipped = ratio * advantages[indices]
                clipped = ratio.clamp(1 - self.clip_epsilon, 1 + self.clip_epsilon) * advantages[indices]
                policy_loss = -torch.minimum(unclipped, clipped).mean()

                # Critic'in tahmin hatasini hesapla.
                value_loss = (self.critic(states[indices]) - returns[indices]).square().mean()
                loss = policy_loss + 0.5 * value_loss

                # Actor ve critic'i guncelle.
                self.optimizer.zero_grad()
                loss.backward()
                nn.utils.clip_grad_norm_(self.parameters, 0.5)
                self.optimizer.step()

                policy_losses.append(policy_loss.item())
                value_losses.append(value_loss.item())

        # Sonraki egitim icin buffer'i bosalt.
        self.buffer.clear()
        return {"policy_loss": sum(policy_losses) / len(policy_losses),
                "value_loss": sum(value_losses) / len(value_losses)}
