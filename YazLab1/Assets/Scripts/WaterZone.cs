using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Suya giris ve cikisi can sistemine bildir.
public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Agent"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.EnterWater(1.0f); // Ateşi söndürür ve saniyede +1 can doldurur
            }
        }
    }

    // Sudan ayrilinca suyun sagladigi iyilesmeyi durdur.
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Agent"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ExitWater(); // Sudan çıkınca can dolması durur
            }
        }
    }
}