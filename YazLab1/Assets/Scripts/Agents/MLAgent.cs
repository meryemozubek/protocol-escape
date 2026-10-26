using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

// ML-Agent hareket ve karar mekanizmasi.
[RequireComponent(typeof(Rigidbody2D))]
public class MLAgent : Agent
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Reward System")]
    [SerializeField] private AgentRewardConfig rewardConfig;

    private AgentRewardPolicy rewardPolicy;

    [Header("Birinci Buton")]
    [SerializeField] private Transform targetButton;
    [SerializeField] private TrainingPlayerPassSimulator targetPassSimulator;

    [Header("Ikinci Buton")]
    [SerializeField] private Transform targetButton2;
    [SerializeField] private TrainingPlayerPassSimulator targetPassSimulator2;

    [Header("Ucuncu Buton")]
    [SerializeField] private Transform targetButton3;
    [SerializeField] private TrainingPlayerPassSimulator targetPassSimulator3;

    private Rigidbody2D rb;
    private PlayerHealth health;

    private Transform[] targetButtons;
    private TrainingPlayerPassSimulator[] passSimulators;

    private Vector2 startPosition;
    private int currentButtonIndex;
    private bool rewardGiven;

    private bool fullHealthRewardGiven;
    private float burningPenaltyTimer;

    // Baslangic ayarlari.
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();

        // Odul ayarlarini yukle.
        if (rewardConfig == null)
        {
            Debug.LogError("Reward Config atanmamis.");
            enabled = false;
            return;
        }

        rewardPolicy = new AgentRewardPolicy(
            rewardConfig.ButtonReward,
            rewardConfig.CompletionReward,
            rewardConfig.FirePenalty,
            rewardConfig.ExtinguishReward,
            rewardConfig.TimePenaltyPerSecond,
            rewardConfig.DeathPenalty,
            rewardConfig.BurningPenaltyPerTick,
            rewardConfig.FullHealthReward,
            rewardConfig.FireReentryPenalty,
            rewardConfig.DryWaterEntryPenalty
        );

        // Ajanin ilk konumunu kaydet.
        startPosition = rb.position;

        // Butonlari gorev sirasina gore kaydet.
        targetButtons = new Transform[]
        {
            targetButton,
            targetButton2,
            targetButton3
        };

        passSimulators = new TrainingPlayerPassSimulator[]
        {
            targetPassSimulator,
            targetPassSimulator2,
            targetPassSimulator3
        };

        // Ajanin olum olayini dinle.
        if (health != null)
        {
            health.OnPlayerDied += HandleAgentDeath;

            // Yanma ve sonme olaylarini dinle.
            health.OnBurnStarted += HandleBurnStarted;
            health.OnFireReentered += HandleFireReentered;
            health.OnExtinguished += HandleExtinguished;

            // Su ve can yenileme olaylarini dinle.
            health.OnWaterEnteredWithoutFire += HandleDryWaterEntry;
            health.OnHealthRestoredInWater += HandleFullHealth;
        }
    }

    // Yeni egitim bolumunu baslatir.
    public override void OnEpisodeBegin()
    {
        // Gorev sirasini ve odul bilgisini sifirla.
        currentButtonIndex = 0;
        rewardGiven = false;

        // Can yenileme odulunu ve yanma sayacini sifirla.
        fullHealthRewardGiven = false;
        burningPenaltyTimer = 0f;

        // Ajanin hareketini durdur ve baslangica dondur.
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.position = startPosition;

        // Can ve yanma durumlarini sifirla.
        if (health != null)
        {
            health.ResetForEpisode();
        }

        // Uc butonun gecis bilgilerini sifirla.
        foreach (TrainingPlayerPassSimulator simulator in passSimulators)
        {
            if (simulator != null)
            {
                simulator.ResetForEpisode();
            }
        }

        Debug.Log("Yeni episode basladi. Tum gorevler sifirlandi.");
    }

    // Hedef butonun ajana gore konumunu gozlemler.
    public override void CollectObservations(VectorSensor sensor)
    {
        if (currentButtonIndex >= targetButtons.Length)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Transform currentTarget = targetButtons[currentButtonIndex];

        if (currentTarget == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector2 difference = (Vector2)currentTarget.position - rb.position;

        sensor.AddObservation(difference.x / 20f);
        sensor.AddObservation(difference.y / 20f);

        // Debug.Log($"Hedef farki: X={difference.x:F2}, Y={difference.y:F2}");
    }

    // Agent'in aksiyonlarini uygular.
    public override void OnActionReceived(ActionBuffers actions)
    {
        float x = actions.ContinuousActions[0];
        float y = actions.ContinuousActions[1];

        Vector2 direction = new Vector2(x, y);
        direction = Vector2.ClampMagnitude(direction, 1f);

        rb.MovePosition(
            rb.position + direction * moveSpeed * Time.fixedDeltaTime
        );

        // Siradaki butonun gecis durumunu kontrol et.
        if (currentButtonIndex >= passSimulators.Length)
            return;

        TrainingPlayerPassSimulator currentSimulator =
            passSimulators[currentButtonIndex];

        if (currentSimulator != null &&
            currentSimulator.HasPlayerPassed &&
            !rewardGiven)
        {
            float buttonReward = rewardPolicy.GetButtonReward(true, rewardGiven);

            rewardGiven = true;

            AddReward(buttonReward);

            Debug.Log(
                $"Buton {currentButtonIndex + 1} tamamlandi. Odul: {buttonReward}"
            );

            // Siradaki goreve gec.
            currentButtonIndex++;

            // Son buton tamamlandiysa bolumu bitir.
            if (currentButtonIndex >= targetButtons.Length)
            {
                float completionReward = rewardPolicy.GetCompletionReward(true);

                AddReward(completionReward);

                Debug.Log($"Tum butonlar tamamlandi. Bonus: {completionReward}");

                EndEpisode();
                return;
            }

            // Siradaki butonun onceki gecis bilgisini temizle.
            if (passSimulators[currentButtonIndex] != null)
            {
                passSimulators[currentButtonIndex].ResetForEpisode();
            }

            rewardGiven = false;

            Debug.Log(
                $"Yeni hedef: Button_0{currentButtonIndex + 1}"
            );
        }
    }

    // Fizik zamanina gore surekli odul ve cezalari uygula.
    private void FixedUpdate()
    {
        if (rewardPolicy == null || health == null)
            return;

        // Dogru butonda oyuncu bekleniyorsa zaman cezasi verme.
        bool waitingAtCorrectButton = false;

        if (currentButtonIndex < passSimulators.Length)
        {
            TrainingPlayerPassSimulator currentSimulator =
                passSimulators[currentButtonIndex];

            waitingAtCorrectButton =
                currentSimulator != null && currentSimulator.IsWaiting;
        }

        float timePenalty = rewardPolicy.GetTimePenalty(
            Time.fixedDeltaTime,
            waitingAtCorrectButton
        );

        AddReward(timePenalty);

        // Yanmiyorsa yanma sayacini sifirla.
        if (!health.IsBurning)
        {
            burningPenaltyTimer = 0f;
            return;
        }

        burningPenaltyTimer += Time.fixedDeltaTime;

        float interval = Mathf.Max(
            0.01f,
            rewardConfig.BurningPenaltyInterval
        );

        // Her tamamlanan yanma periyodunda ceza uygula.
        while (burningPenaltyTimer >= interval)
        {
            burningPenaltyTimer -= interval;

            float penalty = rewardPolicy.GetBurningPenalty();

            AddReward(penalty);

            Debug.Log($"Hala yaniyor! Ceza: {penalty}");
        }
    }

    // Yeni yanma basladiginda ceza ver.
    private void HandleBurnStarted()
    {
        burningPenaltyTimer = 0f;

        float firePenalty = rewardPolicy.GetFirePenalty(true);

        AddReward(firePenalty);

        Debug.Log($"Yandin! Ilk ceza: {firePenalty}");
    }

    // Yaniyorken tekrar atese temas edildiginde ceza ver.
    private void HandleFireReentered()
    {
        float penalty = rewardPolicy.GetFireReentryPenalty(true);

        AddReward(penalty);

        Debug.Log($"Yanarken tekrar atese girdin. Ceza: {penalty}");
    }

    // Ajan suyla sondugunde odul ver.
    private void HandleExtinguished()
    {
        burningPenaltyTimer = 0f;

        float extinguishReward =
            rewardPolicy.GetExtinguishReward(true, true);

        AddReward(extinguishReward);

        Debug.Log($"Ates sondu! Odul: +{extinguishReward}");
    }

    // Ajan yanmazken suya girerse ceza ver.
    private void HandleDryWaterEntry()
    {
        float penalty = rewardPolicy.GetDryWaterEntryPenalty(false);

        AddReward(penalty);

        Debug.Log($"Yanmazken suya girdin. Ceza: {penalty}");
    }

    // Suda can tamamen doldugunda bir kez odul ver.
    private void HandleFullHealth()
    {
        float reward = rewardPolicy.GetFullHealthReward(
            true,
            fullHealthRewardGiven
        );

        if (reward == 0f)
            return;

        fullHealthRewardGiven = true;

        AddReward(reward);

        Debug.Log($"Can tamamen doldu! Odul: +{reward}");
    }

    // Ajan oldugunde ceza ver ve bolumu bitir.
    private void HandleAgentDeath()
    {
        float deathPenalty = rewardPolicy.GetDeathPenalty();

        AddReward(deathPenalty);

        Debug.Log($"ML-Agent oldu. Ceza: {deathPenalty}");

        EndEpisode();
    }

    // WASD ile manuel kontrol.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        actions[0] = 0f;
        actions[1] = 0f;

        if (Keyboard.current == null) return;

        var keyboard = Keyboard.current;

        if (keyboard.aKey.isPressed) actions[0] = -1f;
        if (keyboard.dKey.isPressed) actions[0] = 1f;
        if (keyboard.sKey.isPressed) actions[1] = -1f;
        if (keyboard.wKey.isPressed) actions[1] = 1f;
    }

    // Olum olayinin baglantisini temizle.
    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnPlayerDied -= HandleAgentDeath;

            // Yanma ve sonme olaylarinin baglantisini temizle.
            health.OnBurnStarted -= HandleBurnStarted;
            health.OnFireReentered -= HandleFireReentered;
            health.OnExtinguished -= HandleExtinguished;

            // Su ve can yenileme olaylarinin baglantisini temizle.
            health.OnWaterEnteredWithoutFire -= HandleDryWaterEntry;
            health.OnHealthRestoredInWater -= HandleFullHealth;
        }
    }
}