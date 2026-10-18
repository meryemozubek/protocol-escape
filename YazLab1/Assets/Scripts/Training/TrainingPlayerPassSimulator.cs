
using System.Collections;
using UnityEngine;

public class TrainingPlayerPassSimulator : MonoBehaviour
{
    // Oyuncunun kapidan gecmesi icin rastgele bekleme suresi.
    [SerializeField] private float minDelay = 0f;
    [SerializeField] private float maxDelay = 10f;

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

        passCoroutine = StartCoroutine(SimulatePlayerPass());
    }

    private IEnumerator SimulatePlayerPass()
    {
        // 0-10 saniye arasinda rastgele sure belirlenir.
        float delay = Random.Range(minDelay, maxDelay);

        Debug.Log($"Oyuncunun gecisi bekleniyor. Sure: {delay:F2} saniye.");

        yield return new WaitForSeconds(delay);

        // Bekleme tamamlaninca oyuncunun gectigi kabul edilir.
        HasPlayerPassed = true;
        passCoroutine = null;

        Debug.Log("Oyuncu kapidan gecti. Gecis onaylandi.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Agent") || HasPlayerPassed)
            return;

        // Ajan erken ayrilirsa bekleme iptal edilir.
        if (passCoroutine != null)
        {
            StopCoroutine(passCoroutine);
            passCoroutine = null;

            Debug.Log("ML-Agent butondan erken ayrildi. Gecis iptal edildi.");
        }
    }

    public void ResetForEpisode()
    {
        // Devam eden beklemeyi durdur.
        if (passCoroutine != null)
        {
            StopCoroutine(passCoroutine);
            passCoroutine = null;
        }

        HasPlayerPassed = false;

        Debug.Log("Yeni egitim denemesi basladi. Gecis bilgisi sifirlandi.");
    }
}
