
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

// ML-Agent hareket ve karar mekanizmasi.
[RequireComponent(typeof(Rigidbody2D))]
public class MLAgent : Agent
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;

    // Baslangic ayarlari.
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
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
    }

    // WASD ile manuel kontrol
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
}
