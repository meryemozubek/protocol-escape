
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class VictoryScreenController : MonoBehaviour
{
    [Header("Başarı Ekranı")]
    public TMP_Text timeText;
    public TMP_Text healthText;
    public PlayerHealth playerHealth;

    private static bool retryRequested = false;

    // Başarı ekranı açıldığında bilgileri güncelle
    private void OnEnable()
    {
        // Oyun süresini göster
        if (timeText != null && GameTimer.Instance != null)
        {
            float sure = GameTimer.Instance.ElapsedTime;

            int dakika = Mathf.FloorToInt(sure / 60f);
            int saniye = Mathf.FloorToInt(sure % 60f);

            timeText.text = $"{dakika:00}:{saniye:00}";
        }

        // Oyuncunun kalan canını göster
        if (healthText != null && playerHealth != null)
        {
            healthText.text = playerHealth.CurrentHealth.ToString();

            Debug.Log(
                "Başarı ekranı - Kalan can: " +
                playerHealth.CurrentHealth
            );
        }
        else
        {
            Debug.LogWarning(
                "HealthText veya PlayerHealth bağlantısı eksik!"
            );
        }
    }

    // TEKRAR OYNA - Doğrudan haritayı yeniden başlat
    public void RetryGame()
    {
        retryRequested = true;
        Time.timeScale = 1f;

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // ANA MENÜ - Ana menüye dön
    public void ReturnToMainMenu()
    {
        retryRequested = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    // Sahne yeniden yüklendiğinde ana menüyü atla
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (!retryRequested)
            return;

        retryRequested = false;

        MainMenuController menuController =
            Object.FindObjectOfType<MainMenuController>();

        if (menuController != null)
        {
            menuController.PlayGame();

            Debug.Log(
                "TEKRAR OYNA: Harita doğrudan başlatıldı."
            );
        }
        else
        {
            Debug.LogWarning(
                "MainMenuController bulunamadı!"
            );
        }
    }
}
