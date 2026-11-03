using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 

// Can degistiginde ekrandaki mevcut ve maksimum can yazisini guncelle.
public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
         
            // Her kare sorgulamak yerine can degisimi olayini dinle.
            playerHealth.OnHealthChanged += UpdateHealthUI;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
           
            // Panel kapaninca aboneligi kaldir; eski nesneye bildirim gitmesin.
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth} / {maxHealth}";
        }
    }
}