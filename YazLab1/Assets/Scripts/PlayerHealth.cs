using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 1.0f; // Hasar sonrası dokunulmazlık süresi 

    [Header("Can Yenileme (Regen) Ayarları")]
    [SerializeField] private bool enableRegen = true;    // Zamanla can dolması açık mı?
    [SerializeField] private float regenDelay = 3.0f;     // Hasar aldıktan kaç sn sonra yenileme başlasın?
    [SerializeField] private float regenInterval = 2.0f;  // Kaç saniyede bir +1 can eklensin?

    private int currentHealth;
    private bool isInvulnerable = false;
    private Coroutine regenCoroutine;
    private Coroutine burnCoroutine;
    private Coroutine waterHealCoroutine;

  
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Normal Tuzak / Hasar Alma
    public void TakeDamage(int amount)
    {
        if (isInvulnerable || currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvulnerabilityRoutine());

        // Hasar alınınca otomatik regen süresini baştan başlat
        if (enableRegen)
        {
            ResetAndStartRegen();
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0 || currentHealth >= maxHealth) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    #region ATEŞ VE SU MEKANİZMASI

    // Ateş Çukuruna değince tetiklenir: Suya girene kadar saniyede 1 hasar verir
    public void StartBurning(float damageInterval = 1.0f)
    {
        // Yandığı sırada otomatik yenilenmeyi durdur
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);

        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        burnCoroutine = StartCoroutine(BurnRoutine(damageInterval));
    }

    private IEnumerator BurnRoutine(float interval)
    {
        while (currentHealth > 0)
        {
            yield return new WaitForSeconds(interval);
            
            currentHealth -= 1;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
                yield break;
            }
        }
    }

    // Suya girince tetiklenir: Ateşi söndürür ve suyun içindeyken canı doldurur
    public void EnterWater(float healInterval = 1.0f)
    {
        // 1. Ateşi söndür
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;
            Debug.Log("Suya girildi, ateş söndü!");
        }

        // 2. Suda kaldığı sürece can yenilemeyi başlat
        if (waterHealCoroutine != null) StopCoroutine(waterHealCoroutine);
        waterHealCoroutine = StartCoroutine(WaterHealRoutine(healInterval));
    }

    // Sudan çıkınca can yenilemeyi durdurur
    public void ExitWater()
    {
        if (waterHealCoroutine != null)
        {
            StopCoroutine(waterHealCoroutine);
            waterHealCoroutine = null;
        }

        // Sudan çıkıldığında can hala tam değilse normal otomatik regen başlasın
        if (enableRegen && currentHealth < maxHealth)
        {
            ResetAndStartRegen();
        }
    }

    private IEnumerator WaterHealRoutine(float interval)
    {
        while (currentHealth < maxHealth && currentHealth > 0)
        {
            yield return new WaitForSeconds(interval);
            Heal(1);
        }
    }

    #endregion

    #region REGEN (OTOMATİK YENİLEME) MEKANİZMASI

    private void ResetAndStartRegen()
    {
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenerateHealthRoutine());
    }

    private IEnumerator RegenerateHealthRoutine()
    {
        yield return new WaitForSeconds(regenDelay);

        while (currentHealth < maxHealth && currentHealth > 0)
        {
            Heal(1);

            if (currentHealth < maxHealth)
            {
                yield return new WaitForSeconds(regenInterval);
            }
        }

        regenCoroutine = null;
    }

    #endregion

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    private void Die()
    {
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        if (waterHealCoroutine != null) StopCoroutine(waterHealCoroutine);
        
        OnPlayerDied?.Invoke();
        gameObject.SetActive(false);
    }
}