using System.Collections.Generic;
using UnityEngine;

// Attached to DemoTestV2 and BirlesikOyun. Gameplay scripts remain responsible for movement and switches.
[DefaultExecutionOrder(100)]
public sealed class DemoV2Visuals : MonoBehaviour
{
    public GameObject player;
    public GameObject robot;
    public Sprite[] engineerFrames;
    public Sprite[] robotFrames;
    public Sprite buttonReleased;
    public Sprite buttonPressed;
    public Sprite[] doorFrames;
    private sealed class Walker
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Sprite[] frames;
        public Vector3 previous;
        public int direction;
        public float phase;
        public float lastMove = -1;
    }
    private readonly List<Walker> walkers = new List<Walker>();
    private readonly List<Collider2D> buttons = new List<Collider2D>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private sealed class DoorView
    {
        public DoorScript logic;
        public SpriteRenderer renderer;
        public Collider2D[] colliders;
        public float progress;
    }
    private readonly List<DoorView> doors = new List<DoorView>();
    private ContactFilter2D filter;
    private void Awake()
    {
        if (gameObject.scene.name != "DemoTestV2" && gameObject.scene.name != "BirlesikOyun") { enabled = false; return; }
        AddWalker(player, engineerFrames);
        AddWalker(robot, robotFrames);
        filter = new ContactFilter2D();
        filter.NoFilter();
        foreach (var button in FindObjectsOfType<SwitchTrigger>())
            if (button.gameObject.scene == gameObject.scene && button.TryGetComponent<Collider2D>(out var col)) buttons.Add(col);
        foreach (var door in FindObjectsOfType<DoorScript>())
        {
            if (door.gameObject.scene != gameObject.scene || doorFrames == null || doorFrames.Length != 5) continue;
            // Retain public door commands and state, but let this scene own the visual opening.
            // Disabling the component stops its Update translation; existing switch references still work.
            door.enabled = false;
            doors.Add(new DoorView { logic = door, renderer = door.GetComponent<SpriteRenderer>(), colliders = door.GetComponents<Collider2D>(), progress = door.IsOpen ? 1f : 0f });
        }
    }
    private void AddWalker(GameObject target, Sprite[] frames)
    {
        if (target == null || frames == null || frames.Length != 12) return;
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null) return;
        walkers.Add(new Walker { transform = target.transform, renderer = renderer, frames = frames, previous = target.transform.position });
    }
    private void LateUpdate()
    {
        if (Time.timeScale == 0) return;
        foreach (var door in doors)
        {
            if (door.logic == null) continue;
            door.progress = Mathf.MoveTowards(door.progress, door.logic.IsOpen ? 1f : 0f, Time.deltaTime * 3f);
            int frame = door.progress >= 0.999f ? 4 : Mathf.Min(3, Mathf.FloorToInt(door.progress * 4f));
            if (door.renderer != null) door.renderer.sprite = doorFrames[frame];
            foreach (var col in door.colliders) if (col != null) col.enabled = door.progress < 0.999f;
        }
        foreach (var w in walkers)
        {
            var delta = w.transform.position - w.previous;
            w.previous = w.transform.position;
            if (delta.sqrMagnitude > 1f) { w.phase = 0; w.lastMove = -1; }
            else if (delta.sqrMagnitude > 0.000001f)
            {
                w.direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x < 0 ? 2 : 3) : (delta.y < 0 ? 0 : 1);
                w.lastMove = Time.time;
            }
            bool walking = Time.time - w.lastMove < 0.08f;
            w.phase = walking ? w.phase + Time.deltaTime * 8f : 0;
            int step = (int)w.phase % 4;
            int frame = walking ? (step == 0 ? 1 : step == 2 ? 2 : 0) : 0;
            w.renderer.sprite = w.frames[w.direction * 3 + frame];
        }
        foreach (var col in buttons)
        {
            if (col == null || !col.isActiveAndEnabled) continue;
            overlaps.Clear();
            col.OverlapCollider(filter, overlaps);
            bool pressed = false;
            foreach (var other in overlaps)
                if (other != null && (other.CompareTag("Player") || other.CompareTag("Agent"))) { pressed = true; break; }
            var renderer = col.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = pressed ? buttonPressed : buttonReleased;
        }
    }
}
