import torch
from torch import nn
from torch.distributions import Normal, TanhTransform, TransformedDistribution  # Gauss, donusum uygula, tanh ile sinirlandir


# Gozlemleri alip ajanin X ve Y yonunde yapacagi hareketi sec.
class Actor(nn.Module):
    def __init__(self, observation_size=31, action_size=2):
        super().__init__()
        self.observation_size = observation_size
        self.action_size = action_size
        
        # Varsayilan 31 gozlemi iki gizli katmandan gecir.
        self.fc1 = nn.Linear(observation_size, 256)
        self.fc2 = nn.Linear(256, 256)
        self.fc3 = nn.Linear(256, action_size)
        self.silu = nn.SiLU()
        
        # X ve Y icin standart sapma parametreleri.
        self.std_parameter = nn.Parameter(torch.zeros(action_size))

    def forward(self, x):
        x = self.silu(self.fc1(x))
        x = self.silu(self.fc2(x))
        
        # Standart sapma pozitif kalsin ve cok buyuk ya da kucuk olmasin.
        return self.fc3(x), self.std_parameter.clamp(-5.0, 2.0).exp()

    def get_action(self, x, deterministic=False):
        mean, std = self.forward(x)
        
        # Hareket dagilimini -1 ile 1 arasina sinirla.
        distribution = TransformedDistribution(Normal(mean, std), [TanhTransform(cache_size=1)])
        
        # Degerlendirmede ortalamayi kullan; egitimde farkli hareketleri dene.
        action = torch.tanh(mean) if deterministic else distribution.sample()
        
        # Sinir degerlerde sayisal hata olusmasini engelle.
        action = action.clamp(-0.999999, 0.999999)
        
        # X ve Y degerlerini toplayarak tek bir hareketin olasiligini hesapla.
        return action, distribution.log_prob(action).sum(dim=-1)
