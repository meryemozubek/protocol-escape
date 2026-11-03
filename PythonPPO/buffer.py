# Bir PPO guncellemesine kadar toplanan hareketleri ve sonuclarini tut.
class RolloutBuffer:

    def __init__(self):
        
        # Ajanin yaptiklarini.
        self.observations = []
        self.actions = []
        self.log_probs = []
        self.rewards = []
        self.next_observations = []
        
        self.terminated = []             # Gorev bitti veya ajan oldu mu?
        
        self.done = []                   # Oyun bolumu bitti mi?


    def add(self, observation, action, log_prob, reward, next_observation,
            terminated, done):
        
        # Yeni hareketin bilgilerini listelere ekle.
        self.observations.append(observation)
        self.actions.append(action)
        self.log_probs.append(log_prob)
        self.rewards.append(reward)
        self.next_observations.append(next_observation)
        self.terminated.append(terminated)
        self.done.append(done)

    def clear(self):
        
        # Kullanilan bilgileri temizle.
        self.observations.clear()
        self.actions.clear()
        self.log_probs.clear()
        self.rewards.clear()
        self.next_observations.clear()
        self.terminated.clear()
        self.done.clear()
