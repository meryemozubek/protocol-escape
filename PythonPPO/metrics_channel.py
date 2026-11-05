import json
from collections import deque
from uuid import UUID
from mlagents_envs.side_channel.side_channel import SideChannel
from mlagents_envs.side_channel.outgoing_message import OutgoingMessage

# Unity tarafindaki ChannelGuid ile ayni olmali; iki taraf bu kimlikle eslesir.
CHANNEL_ID = UUID('7ed96650-9c2b-4d29-8078-091c21234bcf')


# Gozlem ve odul disindaki egitim bilgilerini Unity ile paylas.
class TrainingMetricsChannel(SideChannel):
    def __init__(self):
        super().__init__(CHANNEL_ID)
        self.environment_info = None
        # Bolum sonu bilgilerini geldikleri sirayla sakla.
        self.episode_summaries = deque()

    # Unity mesajini JSON olarak oku; ortam ayari ile bolum sonucunu ayir.
    def on_message_received(self, message):
        payload = json.loads(message.read_string())
        if payload.get('kind') == 'environment_info':
            # Odul ayarlari mesajin icinde ayri bir JSON metni olarak geliyor.
            payload['reward_settings'] = json.loads(payload.pop('reward_settings_json'))
            self.environment_info = payload
        elif payload.get('kind') == 'episode_summary':
            self.episode_summaries.append(payload)

    # Butonlarda beklenecek sure araligini Unity tarafina gonder.
    def configure_wait_range(self, minimum, maximum):
        message = OutgoingMessage()
        message.write_string(json.dumps({'kind': 'set_wait_range',
                                         'wait_min': minimum, 'wait_max': maximum}))
        self.queue_message_to_send(message)

    # En eski bolum sonucunu bir kez kullan; mesaj yoksa None don.
    def pop_summary(self):
        return self.episode_summaries.popleft() if self.episode_summaries else None
