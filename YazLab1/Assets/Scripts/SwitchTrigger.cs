using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Butona girilince bagli kapiyi ac, cikilinca gecikmeli kapat.
public class SwitchTrigger : MonoBehaviour
{
    [Header("Hedef Kapi Baglantisi")]
    [SerializeField] private DoorScript targetDoor; // Baglanacak kapi script'i

    [Header("Tetikleyici Ayarlari")]
    [SerializeField] private bool destroyOnUse = false; 
    [SerializeField] private float closeDelay = 1.5f; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("Player") || other.CompareTag("Agent"))
        {
            // Eger arkada calisan bir kapatma sayaci varsa iptal et
            CancelInvoke(nameof(CloseDoorWithDelay));

            if (targetDoor != null)
            {
                targetDoor.OpenDoor(); 
            }

            // Tek kullanim seciliyse butonu devre disi birak.
            if (destroyOnUse)
            {
                gameObject.SetActive(false); 
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Agent"))
        {
            if (targetDoor != null && !destroyOnUse)
            {
                // Belirlenen sure kadar bekleyip kapiyi kapatma sayacini baslat
                Invoke(nameof(CloseDoorWithDelay), closeDelay);
            }
        }
    }

   
    private void CloseDoorWithDelay()
    {
        if (targetDoor != null)
        {
            targetDoor.CloseDoor(); 
        }
    }
}