using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private Vector3 openOffset = new Vector3(0, 3f, 0);
    [SerializeField] private float moveSpeed = 3f;

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

    public void CloseDoor()
    {
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