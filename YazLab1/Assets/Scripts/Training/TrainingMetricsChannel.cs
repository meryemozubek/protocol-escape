using System;
using Unity.MLAgents.SideChannels;
using UnityEngine;

// Ortam ve bolum bilgilerini Python tarafindaki ozel kanala aktar.
public class TrainingMetricsChannel : SideChannel
{
    // Python metrics_channel.py icindeki CHANNEL_ID ile ayni olmali.
    public const string ChannelGuid = "7ed96650-9c2b-4d29-8078-091c21234bcf";
    public event Action<string> CommandReceived;
    public TrainingMetricsChannel() { ChannelId = new Guid(ChannelGuid); }
    // Veriyi JSON metnine cevir ve sonraki haberlesmede gonderilmek uzere kuyruga koy.
    public void Send(object payload)
    {
        using (var message = new OutgoingMessage())
        {
            message.WriteString(JsonUtility.ToJson(payload));
            QueueMessageToSend(message);
        }
    }
    // Gelen komutu ajana bildir; ayarlari ajan tarafinda degistir.
    protected override void OnMessageReceived(IncomingMessage message)
    {
        CommandReceived?.Invoke(message.ReadString());
    }
}
[Serializable]
// Modelin sahneyle uyumunu kontrol etmek icin gonderilen ayarlar.
public class TrainingEnvironmentInfo
{
    public string kind = "environment_info";
    public string profile = AgentRewardConfig.ProfileId;
    public int vector_observation_size = 4;
    public int expected_observation_size = 31;
    public int max_step;
    public int decision_period;
    public float fixed_delta_time;
    public float move_speed;
    public float observation_range;
    public string reward_settings_json;
    public float[] wait_min;
    public float[] wait_max;
    public float[] button_x;
    public float[] button_y;
}
[Serializable]
// Bir bolumun buton sonuclari ve bitis nedeni.
public class TrainingEpisodeSummary
{
    public string kind = "episode_summary";
    public int unity_episode;
    public int completed_buttons;
    public int[] reached_counts;
    public int[] completion_counts;
    public int[] wait_cancellations;
    public float[] minimum_target_distances;
    public float simulation_seconds;
    public string termination_reason;
}
[Serializable]
// Pythonun buton bekleme surelerini degistirmek icin gonderdigi komut.
public class TrainingCommand
{
    public string kind;
    public float wait_min;
    public float wait_max;
}
