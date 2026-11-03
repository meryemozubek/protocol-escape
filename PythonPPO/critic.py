from torch import nn


# V(s) tahmin eder.
class Critic(nn.Module):
    def __init__(self, observation_size=31):
        super().__init__()
        self.observation_size = observation_size
        
        # Actor ile ayni gozlemleri kullan; cikista hareket yerine tek deger uret.
        self.fc1 = nn.Linear(observation_size, 256)
        self.fc2 = nn.Linear(256, 256)
        self.fc3 = nn.Linear(256, 1)
        self.silu = nn.SiLU()

    def forward(self, x):
        # Sondaki tek elemanli boyutu kaldir; her durum icin bir tahmin kalsin.
        return self.fc3(self.silu(self.fc2(self.silu(self.fc1(x))))).squeeze(-1)
