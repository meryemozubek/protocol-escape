using UnityEngine;

// Odul ve ceza degerlerini Inspector uzerinden yonetir.
[CreateAssetMenu(fileName = "AgentRewardConfig", menuName = "Protocol Escape/Agent Reward Config")]
public class AgentRewardConfig : ScriptableObject
{
    // Python bu profil adiyla dogru goreve baglandigini kontrol eder.
    public const string ProfileId = "button_task_v2";
    [Header("Gorev tamamlama")]
    [SerializeField] private float buttonReward = 3f;
    [SerializeField] private float button2Reward = 5f;
    [SerializeField] private float button3Reward = 7f;
    // Uc butonun tamami bittiginde buton odullerine ek olarak verilir.
    [SerializeField] private float completionReward = 10f;
    [Header("Simulasyon zamani ve ilerleme")]
    // Hedefe yaklasmayi odullendir; uzaklasma ayni olcekte negatif odul getirir.
    [SerializeField] private float progressRewardScale = 0.05f;
    [SerializeField] private float waitingRewardPerSecond = 0.01f;
    // Gereksiz oyalanmayi azaltmak icin her simulasyon saniyesinde uygulanir.
    [SerializeField] private float timePenaltyPerSecond = -0.005f;
    // Inspector degerlerini odul hesabina salt okunur olarak ac.
    public float ButtonReward => buttonReward;
    public float Button2Reward => button2Reward;
    public float Button3Reward => button3Reward;
    public float CompletionReward => completionReward;
    public float ProgressRewardScale => progressRewardScale;
    public float WaitingRewardPerSecond => waitingRewardPerSecond;
    public float TimePenaltyPerSecond => timePenaltyPerSecond;
}
