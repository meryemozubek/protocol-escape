using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Yanma yalnizca oyuncuya uygulanir; ML-Agent can sistemine dahil degildir.
public class FirePit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Can bileseni varsa yak; yanmanin devamini PlayerHealth yonetir.
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.StartBurning(1.0f); // Saniyede 1 can düşürür
            }
        }
    }
}