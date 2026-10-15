using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
         
            playerHealth.OnHealthChanged += UpdateHealthUI;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
           
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