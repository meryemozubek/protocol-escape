using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private Vector3 openOffset = new Vector3(0, 3f, 0);
    [SerializeField] private float moveSpeed = 3f;

    public bool IsOpen => isOpen;
    public Vector2 PassageDirection => ((Vector2)openOffset).sqrMagnitude > 0.001f
        ? Vector2.Perpendicular((Vector2)openOffset).normalized : Vector2.up;
    private bool keepOpen;
    private Vector3 closedPosition;
    private Vector3 targetPosition;

    private void Start()
    {
        closedPosition = transform.position;
        targetPosition = isOpen ? (closedPosition + openOffset) : closedPosition;
    }

    private void Update()
    {
        // Kapi'yi hedef pozisyona kaydir
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    public void OpenDoor()
    {
        isOpen = true;
        targetPosition = closedPosition + openOffset;
        Debug.Log("Door opened!");
    }

    public void KeepOpen() { keepOpen = true; OpenDoor(); }

    public void CloseDoor()
    {
        if (keepOpen) return;
        isOpen = false;
        targetPosition = closedPosition;
        Debug.Log("Door closed!");
    }

    public void ToggleDoor()
    {
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }
}