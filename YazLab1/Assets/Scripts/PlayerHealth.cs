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

    // Yanma ve su olaylari.
    public event Action OnBurnStarted;
    public event Action OnFireReentered;
    public event Action OnExtinguished;
    public event Action OnWaterEnteredWithoutFire;
    public event Action OnHealthRestoredInWater;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsBurning => burnCoroutine != null;

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
        if (currentHealth <= 0)
            return;

        // Zaten yaniyorsa yanma suresini sifirlama.
        if (burnCoroutine != null)
        {
            OnFireReentered?.Invoke();
            return;
        }

        // Yandığı sırada otomatik yenilenmeyi durdur
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = null;

        burnCoroutine = StartCoroutine(BurnRoutine(damageInterval));

        // Yeni yanma basladigini bildir.
        OnBurnStarted?.Invoke();
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
        if (currentHealth <= 0)
            return;

        bool wasBurning = burnCoroutine != null;

        // 1. Ateşi söndür
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            burnCoroutine = null;

            Debug.Log("Suya girildi, ateş söndü!");
            OnExtinguished?.Invoke();
        }

        // Yanmazken suya giris olayini bildir.
        if (!wasBurning)
        {
            OnWaterEnteredWithoutFire?.Invoke();
        }

        // 2. Suda kaldığı sürece can yenilemeyi başlat
        if (waterHealCoroutine != null) StopCoroutine(waterHealCoroutine);

        waterHealCoroutine = null;

        if (currentHealth < maxHealth)
        {
            waterHealCoroutine = StartCoroutine(WaterHealRoutine(healInterval));
        }
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

            // Can yalnizca suda iyilesirken tam dolduysa bildir.
            if (currentHealth == maxHealth)
            {
                OnHealthRestoredInWater?.Invoke();
            }
        }

        waterHealCoroutine = null;
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

    // Yeni egitim bolumu icin can ve etkileri sifirlar.
    public void ResetForEpisode()
    {
        // Yanma, su ve regen islemlerini durdur.
        StopAllCoroutines();

        regenCoroutine = null;
        burnCoroutine = null;
        waterHealCoroutine = null;

        // Can ve dokunulmazlik durumunu sifirla.
        isInvulnerable = false;
        currentHealth = maxHealth;

        // Can gostergesini guncelle.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        if (burnCoroutine != null) StopCoroutine(burnCoroutine);
        if (waterHealCoroutine != null) StopCoroutine(waterHealCoroutine);

        regenCoroutine = null;
        burnCoroutine = null;
        waterHealCoroutine = null;

        OnPlayerDied?.Invoke();

        // Normal Player kapanir egitim Agent'i aktif kalir.
        if (!CompareTag("Agent"))
        {
            gameObject.SetActive(false);
        }
    }
}