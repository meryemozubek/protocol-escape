using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tuzaga giren oyuncuya belirlenen miktarda hasar ver.
public class Hazard : MonoBehaviour
{
    [Header("Tuzak Ayarları")]
    [SerializeField] private int damageAmount = 1; // tuzağın vereceği hasar miktarı

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // obje "Player" tag'ine sahip mi?
        if (collision.CompareTag("Player"))
        {
            // Player üzerindeki PlayerHealth bileşenini al
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();

            //oyuncuda PlayerHealth script'i varsa hasar ver
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}