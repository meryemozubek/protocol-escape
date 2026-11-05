using UnityEngine;

public class PlayerDoorPassDetector : MonoBehaviour
{
    private DoorScript door;
    private TrainingPlayerPassSimulator task;
    private Transform player;
    private Vector2 center, normal, tangent;
    private float halfWidth, clearance;
    private float entrySide;

    public void Initialize(DoorScript targetDoor, TrainingPlayerPassSimulator targetTask)
    {
        door = targetDoor;
        task = targetTask;
        var bounds = door.GetComponent<Collider2D>().bounds;
        center = bounds.center;
        normal = door.PassageDirection;
        tangent = Vector2.Perpendicular(normal);
        halfWidth = Mathf.Abs(tangent.x) * bounds.extents.x + Mathf.Abs(tangent.y) * bounds.extents.y;
        clearance = Mathf.Abs(normal.x) * bounds.extents.x + Mathf.Abs(normal.y) * bounds.extents.y;
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
        {
            player = obj.transform;
            var collider = obj.GetComponent<Collider2D>();
            if (collider != null)
                clearance += Mathf.Abs(normal.x) * collider.bounds.extents.x + Mathf.Abs(normal.y) * collider.bounds.extents.y;
        }
        clearance += 0.1f;
    }

    private void FixedUpdate()
    {
        if (player != null) Observe(player.position);
    }

    public void Observe(Vector2 position)
    {
        if (task == null || door == null || !task.IsTaskActive || task.HasPlayerPassed)
        { entrySide = 0; return; }
        Vector2 relative = position - center;
        float side = Vector2.Dot(relative, normal);
        // Kapinin yanindan dolasmak veya ayni taraftan geri cikmak gecis sayilmaz.
        if (Mathf.Abs(Vector2.Dot(relative, tangent)) > halfWidth + 0.15f)
        { entrySide = 0; return; }
        if (entrySide == 0)
        {
            if (door.IsOpen && task.IsWaiting && Mathf.Abs(side) >= 0.05f)
                entrySide = Mathf.Sign(side);
            return;
        }
        if (!door.IsOpen) { entrySide = 0; return; }
        if (side * entrySide < -clearance)
        {
            task.NotifyPlayerPassed();
            Debug.Log("[PlayerPass] Gercek oyuncu gecti: " + door.name);
            entrySide = 0;
        }
    }
}
