using UnityEngine;

// Odul ve ceza degerlerini Inspector uzerinden yonetir.
[CreateAssetMenu(
    fileName = "AgentRewardConfig",
    menuName = "Protocol Escape/Agent Reward Config"
)]
public class AgentRewardConfig : ScriptableObject
{
    [Header("Gorev Odulleri")]
    [SerializeField] private float buttonReward = 1f;
    [SerializeField] private float completionReward = 10f;

    [Header("Ates ve Su")]
    [SerializeField] private float firePenalty = -1f;
    [SerializeField] private float fireReentryPenalty = -1f;
    [SerializeField] private float extinguishReward = 2f;
    [SerializeField] private float dryWaterEntryPenalty = -0.25f;

    [Header("Surekli Yanma")]
    [SerializeField] private float burningPenaltyInterval = 0.5f;
    [SerializeField] private float burningPenaltyPerTick = -2f;

    [Header("Can Yenileme")]
    [SerializeField] private float fullHealthReward = 0.5f;

    [Header("Zaman ve Olum")]
    [SerializeField] private float timePenaltyPerSecond = -0.01f;
    [SerializeField] private float deathPenalty = -10f;

    public float ButtonReward => buttonReward;
    public float CompletionReward => completionReward;
    public float FirePenalty => firePenalty;
    public float FireReentryPenalty => fireReentryPenalty;
    public float ExtinguishReward => extinguishReward;
    public float DryWaterEntryPenalty => dryWaterEntryPenalty;
    public float BurningPenaltyInterval => burningPenaltyInterval;
    public float BurningPenaltyPerTick => burningPenaltyPerTick;
    public float FullHealthReward => fullHealthReward;
    public float TimePenaltyPerSecond => timePenaltyPerSecond;
    public float DeathPenalty => deathPenalty;
}