// Odul ve ceza kurallarini hesaplar.
// Unity ML-Agents veya MonoBehaviour'a bagimli degildir.
public class AgentRewardPolicy
{
    private readonly float[] buttonRewards;
    private readonly float completionReward;
    private readonly float progressRewardScale;
    private readonly float waitingRewardPerSecond;
    private readonly float timePenaltyPerSecond;
    // Inspector ayarlarini al; odul hesaplarinda ayni degerleri kullan.
    public AgentRewardPolicy(float buttonReward, float button2Reward, float button3Reward,
        float completionReward, float progressRewardScale,
        float waitingRewardPerSecond, float timePenaltyPerSecond)
    {
        buttonRewards = new[] { buttonReward, button2Reward, button3Reward };
        this.completionReward = completionReward;
        this.progressRewardScale = progressRewardScale;
        this.waitingRewardPerSecond = waitingRewardPerSecond;
        this.timePenaltyPerSecond = timePenaltyPerSecond;
    }
    // Oyuncunun dogru butondan gecisi tamamlandiginda odul ver
    public float GetButtonReward(int buttonIndex, bool alreadyCompleted = false)
    {
        return !alreadyCompleted && buttonIndex >= 0 && buttonIndex < buttonRewards.Length
            ? buttonRewards[buttonIndex] : 0f;
    }
    // Butun gorevler tamamlanmissa ek bitirme odulunu ver.
    public float GetCompletionReward(bool allCompleted)
    {
        return allCompleted ? completionReward : 0f;
    }
    // Onceki uzakliktan yeni uzakligi cikar; hedefe yaklastiysa sonuc pozitif olur.
    public float GetProgressReward(float previousDistance, float currentDistance)
    {
        return progressRewardScale * (previousDistance - currentDistance);
    }
    // Cezayi fizik adiminin suresiyle carp; karar sayisina baglama.
    public float GetTimePenalty(float deltaTime)
    {
        return timePenaltyPerSecond * System.Math.Max(0f, deltaTime);
    }
    // Dogru butonda bekledigi her saniye icin odul hesapla.
    public float GetWaitingReward(float deltaTime, bool waitingAtCorrectButton)
    {
        return waitingAtCorrectButton
            ? waitingRewardPerSecond * System.Math.Max(0f, deltaTime) : 0f;
    }
}
