
using System.Collections;
using UnityEngine;

public class TrainingPlayerPassSimulator : MonoBehaviour
{
    // Oyuncunun kapidan gecmesi icin beklenecek sure araligi.
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;

    // Oyuncunun kapidan gecip gecmedigini belirtir.
    public bool HasPlayerPassed { get; private set; }

    private Coroutine passCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Yalnizca ML-Agent butona bastiginda calisir.
        if (!other.CompareTag("Agent") ||
            HasPlayerPassed ||
            passCoroutine != null)
            return;

        // Oyuncunun kapidan gecmesini simule eder.
        passCoroutine = StartCoroutine(SimulatePlayerPass());
    }

    private IEnumerator SimulatePlayerPass()
    {
        // 1-3 saniye arasinda rastgele bir sure belirlenir.
        float delay = Random.Range(minDelay, maxDelay);

        Debug.Log($"Oyuncunun gecisi bekleniyor. Sure: {delay:F2} saniye.");

        yield return new WaitForSeconds(delay);

        // Oyuncunun kapidan gectigi kabul edilir.
        HasPlayerPassed = true;
        passCoroutine = null;

        Debug.Log("Oyuncu kapidan gecti. Gecis onaylandi.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // ML-Agent butondan ayrilmadiysa islem yapilmaz.
        if (!other.CompareTag("Agent") || HasPlayerPassed)
            return;

        // Erken ayrilma durumunda gecis iptal edilir.
        if (passCoroutine != null)
        {
            StopCoroutine(passCoroutine);
            passCoroutine = null;

            Debug.Log("ML-Agent butondan erken ayrildi. Gecis iptal edildi.");
        }
    }

    public void ResetForEpisode()
    {
        // Devam eden simulasyon varsa durdurulur.
        if (passCoroutine != null)
        {
            StopCoroutine(passCoroutine);
            passCoroutine = null;
        }

        // Yeni egitim denemesi icin gecis bilgisi sifirlanir.
        HasPlayerPassed = false;

        Debug.Log("Yeni egitim denemesi basladi. Gecis bilgisi sifirlandi.");
    }
}
