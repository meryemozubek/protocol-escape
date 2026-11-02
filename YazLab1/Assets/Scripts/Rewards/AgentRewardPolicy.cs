// Odul ve ceza kurallarini hesaplar.
// Unity ML-Agents veya MonoBehaviour'a bagimli degildir.

public class AgentRewardPolicy
{
    private readonly float buttonReward;
    private readonly float completionReward;
    private readonly float firePenalty;
    private readonly float fireReentryPenalty;
    private readonly float extinguishReward;
    private readonly float dryWaterEntryPenalty;
    private readonly float timePenaltyPerSecond;
    private readonly float deathPenalty;
    private readonly float burningPenaltyPerTick;
    private readonly float fullHealthReward;
    private readonly float waitingRewardPerSecond;
    private readonly float earlyExitPenalty;
    private readonly float approachReward;

    public AgentRewardPolicy(
        float buttonReward,
        float completionReward,
        float firePenalty,
        float extinguishReward,
        float timePenaltyPerSecond,
        float deathPenalty,
        float burningPenaltyPerTick,
        float fullHealthReward,
        float fireReentryPenalty,
        float dryWaterEntryPenalty,
        float waitingRewardPerSecond,
        float earlyExitPenalty,
        float approachReward)
    {
        this.buttonReward = buttonReward;
        this.completionReward = completionReward;
        this.firePenalty = firePenalty;
        this.extinguishReward = extinguishReward;
        this.timePenaltyPerSecond = timePenaltyPerSecond;
        this.deathPenalty = deathPenalty;
        this.burningPenaltyPerTick = burningPenaltyPerTick;
        this.fullHealthReward = fullHealthReward;
        this.fireReentryPenalty = fireReentryPenalty;
        this.dryWaterEntryPenalty = dryWaterEntryPenalty;
        this.waitingRewardPerSecond = waitingRewardPerSecond;
        this.earlyExitPenalty = earlyExitPenalty;
        this.approachReward = approachReward;
    }

    // Oyuncunun dogru butondan gecisi tamamlandiginda odul ver
    public float GetButtonReward(bool isExpected, bool alreadyCompleted)
    {
        return isExpected && !alreadyCompleted ? buttonReward : 0f;
    }

    // Tum gorevler ilk kez tamamlandiginda odul ver
    public float GetCompletionReward(bool firstCompletion)
    {
        return firstCompletion ? completionReward : 0f;
    }

    // Yeni yanma olustugunda ceza ver
    public float GetFirePenalty(bool newlyBurning)
    {
        return newlyBurning ? firePenalty : 0f;
    }

    // Zaten yanarken tekrar atese temas edildiginde ceza ver.
    public float GetFireReentryPenalty(bool alreadyBurning)
    {
        return alreadyBurning ? fireReentryPenalty : 0f;
    }

    // Yanma devam ederken her ceza periyodu icin hesapla.
    public float GetBurningPenalty()
    {
        return burningPenaltyPerTick;
    }

    // Yalnizca yanarken gercekten sondugunde odul ver
    public float GetExtinguishReward(bool wasBurning, bool extinguished)
    {
        return wasBurning && extinguished ? extinguishReward : 0f;
    }

    // Yanmazken suya girildiginde ceza ver.
    public float GetDryWaterEntryPenalty(bool wasBurning)
    {
        return !wasBurning ? dryWaterEntryPenalty : 0f;
    }

    // Suda can tamamen doldugunda bir kez odul ver.
    public float GetFullHealthReward(bool healedInWater, bool alreadyRewarded)
    {
        return healedInWater && !alreadyRewarded ? fullHealthReward : 0f;
    }

    // Dogru butonda oyuncuyu beklerken zaman cezasi verme
    public float GetTimePenalty(float deltaTime, bool waitingAtCorrectButton)
    {
        if (waitingAtCorrectButton)
            return 0f;

        return timePenaltyPerSecond * System.Math.Max(0f, deltaTime);
    }

    // Dogru butonda bekledigi her saniye icin odul hesapla.
    public float GetWaitingReward(float deltaTime, bool waitingAtCorrectButton)
    {
        return waitingAtCorrectButton
            ? waitingRewardPerSecond * System.Math.Max(0f, deltaTime)
            : 0f;
    }

    // Erken cikis cezasini buton sirasina gore biraz artir.
    public float GetEarlyExitPenalty(int buttonIndex)
    {
        return earlyExitPenalty * (float)System.Math.Pow(1.01, buttonIndex);
    }

    // Onceki en iyi mesafeyi gecerse yaklasma odulu ver.
    public float GetApproachReward(bool madeProgress)
    {
        return madeProgress ? approachReward : 0f;
    }

    // Olum gerceklestiginde ceza ver
    public float GetDeathPenalty()
    {
        return deathPenalty;
    }
}
