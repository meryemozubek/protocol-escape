using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;

// AgentTraining: ates/su icermeyen, sirali uc buton gorevi.
// ML-Agent hareket ve karar mekanizmasi.
[RequireComponent(typeof(Rigidbody2D))]
public class MLAgent : Agent
{
    [SerializeField] private bool gameplayMode;
    [SerializeField] private DoorScript[] gameplayDoors;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float observationRange = 35f;
    [SerializeField] private AgentRewardConfig rewardConfig;
    [SerializeField] private Transform targetButton;
    [SerializeField] private TrainingPlayerPassSimulator targetPassSimulator;
    [SerializeField] private Transform targetButton2;
    [SerializeField] private TrainingPlayerPassSimulator targetPassSimulator2;
    [SerializeField] private Transform targetButton3;
    [SerializeField] private TrainingPlayerPassSimulator targetPassSimulator3;
    private Rigidbody2D rb;
    private GameObject gameplayMenu;
    private Transform[] targets;
    private TrainingPlayerPassSimulator[] simulators;
    private AgentRewardPolicy rewards;
    private TrainingMetricsChannel metrics;
    private Vector2 startPosition;
    private int currentButtonIndex;
    private int unityEpisode;
    private int[] reachedCounts;
    private int[] completionCounts;
    private int[] cancellations;
    private float[] minimumDistances;
    private float previousTargetDistance;
    private float episodeStartTime;
    private bool ready;
    private bool hasActed;
    private bool summarySent;

    // Baslangic ayarlari.
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        if (gameplayMode)
        {
            gameplayMenu = GameObject.Find("MainMenuPanel");
        }
        // Ajanin ilk konumunu kaydet.
        startPosition = rb.position;
        // Butonlari gorev sirasina gore kaydet.
        targets = new[] { targetButton, targetButton2, targetButton3 };
        simulators = new[] { targetPassSimulator, targetPassSimulator2, targetPassSimulator3 };
        if (rewardConfig == null) throw new InvalidOperationException("Reward Config atanmamis.");
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || simulators[i] == null)
                throw new InvalidOperationException($"Buton {i + 1} referanslari eksik.");
            // Butona varma ve beklemeden ayrilma olaylarini saymak icin dinlemeye basla.
            simulators[i].OnAgentReached += HandleReached;
            if (gameplayMode)
            {
                if (gameplayDoors == null || gameplayDoors.Length != 3 || gameplayDoors[i] == null)
                    throw new InvalidOperationException("Oyun kapisi referansi eksik.");
                simulators[i].UseRealPlayer();
                var detector = new GameObject("PlayerPass_" + (i + 1)).AddComponent<PlayerDoorPassDetector>();
                detector.transform.SetParent(transform.parent);
                detector.Initialize(gameplayDoors[i], simulators[i]);
            }
            simulators[i].OnWaitCancelled += HandleWaitCancelled;
        }
        // Odul ayarlarini yukle.
        rewards = new AgentRewardPolicy(rewardConfig.ButtonReward, rewardConfig.Button2Reward,
            rewardConfig.Button3Reward, rewardConfig.CompletionReward,
            rewardConfig.ProgressRewardScale, rewardConfig.WaitingRewardPerSecond,
            rewardConfig.TimePenaltyPerSecond);
        // Python ile ayni kimlikteki kanali ac; bolum sonu bilgilerini buradan gonderecegiz.
        metrics = new TrainingMetricsChannel();
        metrics.CommandReceived += HandleCommand;
        SideChannelManager.RegisterSideChannel(metrics);
        Application.runInBackground = true;
        ready = true;
    }

    // Yeni egitim bolumunu baslatir.
    public override void OnEpisodeBegin()
    {
        if (!ready) return;
        // Onceki bolum disaridan kesildiyse normal basari gibi gostermeden bildir.
        if (hasActed && !summarySent) SendSummary("external_reset");
        unityEpisode++;
        // Gorev sirasini ve odul bilgisini sifirla.
        currentButtonIndex = 0;
        reachedCounts = new int[3]; completionCounts = new int[3]; cancellations = new int[3];
        minimumDistances = new[] { -1f, -1f, -1f };
        hasActed = false; summarySent = false;
        episodeStartTime = Time.fixedTime;
        // Ajanin hareketini durdur ve baslangica dondur.
        rb.velocity = Vector2.zero; rb.angularVelocity = 0f; rb.position = startPosition;
        // Uc butonun gecis bilgilerini sifirla.
        foreach (var simulator in simulators) simulator.ResetForEpisode();
        // Ilk butonla basla; digerleri sira kendilerine gelene kadar gorevi tamamlayamaz.
        simulators[0].SetTaskActive(true);
        ResetTargetDistance();
        SendEnvironmentInfo();
    }

    // Ajanin cevre ve gorev bilgilerini gozlemler.
    public override void CollectObservations(VectorSensor sensor)
    {
        // Siradaki butonun konumu.
        Vector2 difference = currentButtonIndex < targets.Length && targets[currentButtonIndex] != null
            ? (Vector2)targets[currentButtonIndex].position - rb.position : Vector2.zero;
        float range = Mathf.Max(0.001f, observationRange);
        // Uzakliklari benzer olcege getir; isin gozlemleri ayri sensor tarafindan eklenir.
        sensor.AddObservation(Mathf.Clamp(difference.x / range, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(difference.y / range, -1f, 1f));
        // Gorev ilerlemesi.
        sensor.AddObservation((float)currentButtonIndex / targets.Length);
        // Dogru butonda bekleme durumu.
        sensor.AddObservation(IsWaitingAtTarget() ? 1f : 0f);
    }

    // Agent'in aksiyonlarini uygular.
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!ready || summarySent || (gameplayMode && gameplayMenu != null && gameplayMenu.activeInHierarchy)) return;
        hasActed = true;
        // Siradaki butonun gecis durumunu kontrol et.
        if (currentButtonIndex < simulators.Length && simulators[currentButtonIndex].HasPlayerPassed)
        {
            AddReward(rewards.GetButtonReward(currentButtonIndex, completionCounts[currentButtonIndex] > 0));
            // Buton tamamlanmasini bir kez say ve sonraki hedefe gec.
            completionCounts[currentButtonIndex] = 1;
            // Gorev tamamlansa da buton aktif kalir; robot ayrilinca SwitchTrigger kapiyi kapatir.
            simulators[currentButtonIndex].SetTaskActive(false);
            // Siradaki goreve gec.
            currentButtonIndex++;
            // Son buton tamamlandiysa bolumu bitir.
            if (currentButtonIndex == targets.Length)
            {
                AddReward(rewards.GetCompletionReward(true));
                FinishEpisode("completed", false);
                return;
            }
            // Yeni hedef ve teleport uzakliginin sicramasi shaping odulu uretmez.
            ResetTargetDistance();
            simulators[currentButtonIndex].SetTaskActive(true);
        }
        // Native MaxStep ile ayni sinir; terminalden once telemetriyi kuyruklar.
        if (MaxStep > 0 && StepCount >= MaxStep)
        {
            FinishEpisode("max_step", true);
            return;
        }
        // Capraz hareketin hizini sinirla; iki eksende birden gitmek daha hizli olmasin.
        Vector2 direction = Vector2.ClampMagnitude(
            new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]), 1f);
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    // Fizik zamanina gore surekli odul ve cezalari uygula.
    private void FixedUpdate()
    {
        if (!ready || !hasActed || summarySent || currentButtonIndex >= targets.Length) return;
        // Gerceklesmis fizik hareketinin ilerleme odulu yalnizca burada hesaplanir.
        float distance = Vector2.Distance(rb.position, targets[currentButtonIndex].position);
        AddReward(rewards.GetProgressReward(previousTargetDistance, distance));
        previousTargetDistance = distance;
        minimumDistances[currentButtonIndex] = Mathf.Min(minimumDistances[currentButtonIndex], distance);
        AddReward(rewards.GetTimePenalty(Time.fixedDeltaTime));
        // Yalnizca gerekli bekleme surerken saniye basina odul ver.
        AddReward(rewards.GetWaitingReward(Time.fixedDeltaTime, IsWaitingAtTarget()));
    }

    private bool IsWaitingAtTarget()
    {
        return currentButtonIndex < simulators.Length && simulators[currentButtonIndex] != null && simulators[currentButtonIndex].IsWaiting;
    }
    // Yeni hedefin uzakligini baslangic al; hedef degisimi sahte ilerleme odulu uretmesin.
    private void ResetTargetDistance()
    {
        previousTargetDistance = Vector2.Distance(rb.position, targets[currentButtonIndex].position);
        minimumDistances[currentButtonIndex] = previousTargetDistance;
    }
    // Sadece siradaki butona ulasmayi bolum istatistigine ekle.
    private void HandleReached(TrainingPlayerPassSimulator simulator)
    {
        if (currentButtonIndex < simulators.Length && simulators[currentButtonIndex] == simulator)
            reachedCounts[currentButtonIndex]++;
    }
    // Siradaki butondan erken ayrilma sayisini kaydet.
    private void HandleWaitCancelled(TrainingPlayerPassSimulator simulator)
    {
        if (currentButtonIndex < simulators.Length && simulators[currentButtonIndex] == simulator)
            cancellations[currentButtonIndex]++;
    }
    // Once sonucu Pythona gonder; sonra basari veya zaman asimi olarak bolumu bitir.
    private void FinishEpisode(string reason, bool interrupted)
    {
        SendSummary(reason);
        if (gameplayMode && reason == "completed")
        {
            rb.velocity = Vector2.zero;
            var requester = GetComponent<DecisionRequester>();
            if (requester != null) requester.enabled = false;
            return;
        }
        if (interrupted) EpisodeInterrupted(); else EndEpisode();
    }
    // Tamamlanan butonlari, bekleme iptallerini ve simulasyon suresini paketle.
    private void SendSummary(string reason)
    {
        summarySent = true;
        metrics.Send(new TrainingEpisodeSummary {
            unity_episode = unityEpisode, completed_buttons = currentButtonIndex,
            reached_counts = (int[])reachedCounts.Clone(),
            completion_counts = (int[])completionCounts.Clone(),
            wait_cancellations = (int[])cancellations.Clone(),
            minimum_target_distances = (float[])minimumDistances.Clone(),
            simulation_seconds = Mathf.Max(0f, Time.fixedTime - episodeStartTime),
            termination_reason = reason
        });
        Debug.Log($"[ButtonTask] Bolum {unityEpisode}: buton={currentButtonIndex}/3, neden={reason}");
    }
    // Pythonun model uyumlulugunu denetleyebilmesi icin gercek sahne ayarlarini gonder.
    private void SendEnvironmentInfo()
    {
        var requester = GetComponent<DecisionRequester>();
        var minimum = new float[3]; var maximum = new float[3];
        var x = new float[3]; var y = new float[3];
        for (int i = 0; i < 3; i++)
        {
            minimum[i] = simulators[i].MinimumDelay; maximum[i] = simulators[i].MaximumDelay;
            x[i] = targets[i].position.x; y[i] = targets[i].position.y;
        }
        metrics.Send(new TrainingEnvironmentInfo {
            max_step = MaxStep, decision_period = requester != null ? requester.DecisionPeriod : 1,
            fixed_delta_time = Time.fixedDeltaTime, move_speed = moveSpeed,
            observation_range = observationRange, reward_settings_json = JsonUtility.ToJson(rewardConfig),
            wait_min = minimum, wait_max = maximum, button_x = x, button_y = y
        });
    }
    // Pythondan gelen bekleme araligi komutunu uc butona da uygula.
    private void HandleCommand(string json)
    {
        var command = JsonUtility.FromJson<TrainingCommand>(json);
        if (command != null && command.kind == "set_wait_range")
        {
            foreach (var simulator in simulators) simulator.ConfigureDelays(command.wait_min, command.wait_max);
            SendEnvironmentInfo();
        }
    }
    // WASD ile manuel kontrol.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = 0f; actions[1] = 0f;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.aKey.isPressed) actions[0] = -1f;
        if (keyboard.dKey.isPressed) actions[0] = 1f;
        if (keyboard.sKey.isPressed) actions[1] = -1f;
        if (keyboard.wKey.isPressed) actions[1] = 1f;
    }
    // Olay aboneliklerini ve veri kanalini kapat; sonraki oturuma eski baglanti kalmasin.
    private void OnDestroy()
    {
        if (simulators != null)
            foreach (var simulator in simulators)
                if (simulator != null)
                {
                    simulator.OnAgentReached -= HandleReached;
                    simulator.OnWaitCancelled -= HandleWaitCancelled;
                }
        if (metrics != null)
        {
            metrics.CommandReceived -= HandleCommand;
            SideChannelManager.UnregisterSideChannel(metrics);
        }
    }
}
