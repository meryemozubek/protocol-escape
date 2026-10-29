import torch
from torch import nn
from torch.distributions import Normal, TransformedDistribution, TanhTransform # Gauss, donusum uygula, tanh ile donustur.


class Actor(nn.Module):

    def __init__(self):
        super().__init__()

        self.fc1 = nn.Linear(9 * 4 + 8, 128)

        self.relu = nn.ReLU()

        self.fc2 = nn.Linear(128, 128)

        self.fc3 = nn.Linear(128, 2)

        self.std_parameter = nn.Parameter(torch.zeros(2)) # X ve Y icin standart sapma parametreleri.
        
        
    def forward(self, x):

        x = self.fc1(x)
        x = self.relu(x)

        x = self.fc2(x)
        x = self.relu(x)

        mean = self.fc3(x)

        std = torch.exp(self.std_parameter) # Standart sapmayi pozitif tutar, gradyan descent'i de kolay hesaplanir.

        return mean, std


    def get_action(self, x):

        mean, std = self.forward(x)
    
        normal = Normal(mean, std)
    
        dist = TransformedDistribution(  # Hareket dagilimini -1 ile 1 arasina sinirla.
            normal, [TanhTransform(cache_size=1)]
        )
    
        action = dist.sample()       # Dagilimdan rastgele X ve Y ciktisi alir.
    
        action = action.clamp(-0.999999, 0.999999) # Sinir degerlerde sayisal hata olusmasini engelle.
    
        values = dist.log_prob(action)  # X ve Y hareketlerinin olasilik yogunluklarini hesapla.
    
        log_prob = values.sum(dim=-1)   # X ve Y degerlerini toplayarak tek bir hareketin olasiligini hesapla.
                                        # Log sayesinde toplaniyor ve floating-point en aza indirgeniyor.
    
        return action, log_prob
    