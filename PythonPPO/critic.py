from torch import nn

class Critic(nn.Module):
    
    def __init__(self):
        super().__init__()

        self.fc1 = nn.Linear(9 * 4 + 8, 128)
        
        self.relu = nn.ReLU()
        
        self.fc2 = nn.Linear(128, 128)
        
        self.fc3 = nn.Linear(128, 1)
        

    def forward(self, x):
        
        x = self.relu(self.fc1(x))
        x = self.relu(self.fc2(x))

        value = self.fc3(x)  # V(s) tahmin eder.

        return value.squeeze(-1)  # Sonuclari 2d'den 1d'ye cevirir.




