
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

    // Baslangic ayarlari.
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();

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
        }
    }

    // Yeni egitim bolumunu baslatir.
    public override void OnEpisodeBegin()
    {
        // Gorev sirasini ve odul bilgisini sifirla.
        currentButtonIndex = 0;
        rewardGiven = false;

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
        TrainingPlayerPassSimulator currentSimulator =
            passSimulators[currentButtonIndex];

        if (currentSimulator != null &&
            currentSimulator.HasPlayerPassed &&
            !rewardGiven)
        {
            rewardGiven = true;

            AddReward(1f);

            Debug.Log(
                $"Buton {currentButtonIndex + 1} tamamlandi. Odul: +1"
            );

            // Siradaki goreve gec.
            currentButtonIndex++;

            // Son buton tamamlandiysa bolumu bitir.
            if (currentButtonIndex >= targetButtons.Length)
            {
                AddReward(10f);

                Debug.Log("Tum butonlar tamamlandi. Bonus: +10");

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

    // Ajan oldugunde ceza ver ve bolumu bitir.
    private void HandleAgentDeath()
    {
        AddReward(-1f);

        Debug.Log("ML-Agent oldu. Ceza: -1");

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
        }
    }
}
