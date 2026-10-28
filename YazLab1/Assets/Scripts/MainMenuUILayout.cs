using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Sahne yükleme/geçiş işlemleri için eklendi
using TMPro;

[ExecuteAlways]
public class MainMenuUILayout : MonoBehaviour
{
    [Header("1. Ana Panel")]
    public RectTransform mainMenuPanel;
    public Image menuBackground;
    public RectTransform menuCard;

    [Header("2. Başlık ve Alt Yazı")]
    public TextMeshProUGUI gameTitleText;
    public TextMeshProUGUI subtitleText;

    [Header("3. Menü Butonları ve Paneller")]
    public Button playButton;
    public Button infoButton;
    public Button exitButton;
    public GameObject howToPlayPanel; // Nasıl Oynanır Ekranı

    private readonly Color backgroundColor = new Color32(10, 22, 32, 255);
    private readonly Color cardColor = new Color32(15, 35, 47, 245);
    private readonly Color neonGreen = new Color32(74, 222, 128, 255);
    private readonly Color lightText = new Color32(226, 232, 240, 255);
    private readonly Color mutedText = new Color32(148, 163, 184, 255);

    private void Awake()
    {
        // Oyun başlatıldığında tıklama olaylarını bağla
        if (Application.isPlaying)
        {
            SetupButtonEvents();
        }
    }

    private void Start()
    {
        ApplyLayout();

        // Başlangıçta Nasıl Oynanır panelinin kapalı olduğundan emin ol
        if (Application.isPlaying && howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    public void SetupButtonEvents()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartGame);
        }

        if (infoButton != null)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(OpenHowToPlay);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitGame);
        }
    }

    // NASIL OYNANIR? Butonuna basılınca çalışır
    public void OpenHowToPlay()
    {
        if (howToPlayPanel != null) 
        {
            howToPlayPanel.SetActive(true);

            // Panelin tam ekran yayılmasını ve arkadaki haritayı kapatmasını garantiye alalım
            RectTransform panelRect = howToPlayPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }
        }
        
        if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
    }

    // GERİ DÖN Butonuna basılınca çalışır
    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(true);
    }

    // OYUNA BAŞLA
    public void StartGame()
    {
        // 1. Tüm menü panellerini kapat
        if (mainMenuPanel != null) mainMenuPanel.gameObject.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        
        // 2. Zamanı ve oyunu başlat (Eğer durdurulduysa)
        Time.timeScale = 1f;

        Debug.Log("PROTOCOL: ESCAPE oyunu baslatildi!");

        // NOT: Eğer oyun alanın ayrı bir sahnedeyse alttaki satırın yorumunu kaldırıp sahne adını yazabilirsin:
        // SceneManager.LoadScene("MazeScene");
    }

    // ÇIKIŞ
    public void QuitGame()
    {
        Debug.Log("PROTOCOL: ESCAPE oyunundan cikis yapildi!");
        Application.Quit();
    }

    [ContextMenu("Giriş Ekranını Düzenle")]
    public void ApplyLayout()
    {
        // ANA PANEL
        if (mainMenuPanel != null)
        {
            mainMenuPanel.anchorMin = Vector2.zero;
            mainMenuPanel.anchorMax = Vector2.one;
            mainMenuPanel.offsetMin = Vector2.zero;
            mainMenuPanel.offsetMax = Vector2.zero;
        }

        // ARKA PLAN
        if (menuBackground != null)
        {
            RectTransform bgRect = menuBackground.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            menuBackground.color = backgroundColor;
        }

        // ANA KART
        if (menuCard != null)
        {
            menuCard.anchorMin = new Vector2(0.5f, 0.5f);
            menuCard.anchorMax = new Vector2(0.5f, 0.5f);
            menuCard.pivot = new Vector2(0.5f, 0.5f);
            menuCard.anchoredPosition = Vector2.zero;
            menuCard.sizeDelta = new Vector2(600f, 620f);

            Image cardImage = menuCard.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = cardColor;
            }

            Outline cardOutline = menuCard.GetComponent<Outline>();
            if (cardOutline == null)
            {
                cardOutline = menuCard.gameObject.AddComponent<Outline>();
            }

            cardOutline.effectColor = new Color32(56, 189, 248, 180);
            cardOutline.effectDistance = new Vector2(2f, -2f);
        }

        // OYUN BAŞLIĞI
        if (gameTitleText != null && menuCard != null)
        {
            RectTransform titleRect = gameTitleText.rectTransform;
            titleRect.SetParent(menuCard, false);
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 190f);
            titleRect.sizeDelta = new Vector2(560f, 150f);

            gameTitleText.text = "PROTOCOL:\n<color=#4ADE80>ESCAPE</color>";
            gameTitleText.fontSize = 58;
            gameTitleText.fontStyle = FontStyles.Bold;
            gameTitleText.alignment = TextAlignmentOptions.Center;
            gameTitleText.enableAutoSizing = false;
            gameTitleText.enableWordWrapping = false;
            gameTitleText.color = Color.white;
            gameTitleText.margin = Vector4.zero;
        }

        // ALT BAŞLIK
        if (subtitleText != null && menuCard != null)
        {
            RectTransform subtitleRect = subtitleText.rectTransform;
            subtitleRect.SetParent(menuCard, false);
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.anchoredPosition = new Vector2(0f, 105f);
            subtitleRect.sizeDelta = new Vector2(500f, 35f);

            subtitleText.text = "LABYRINTH OVERRIDE SYSTEM";
            subtitleText.fontSize = 15;
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = mutedText;
            subtitleText.enableAutoSizing = false;
        }

        // MENÜ BUTONLARI
        StyleButton(playButton, "OYUNA BAŞLA", new Vector2(0f, 10f), new Vector2(360f, 60f), neonGreen, new Color32(7, 18, 12, 255), true);
        StyleButton(infoButton, "NASIL OYNANIR?", new Vector2(0f, -65f), new Vector2(360f, 52f), new Color32(23, 33, 43, 255), lightText, false);
        StyleButton(exitButton, "ÇIKIŞ", new Vector2(0f, -132f), new Vector2(360f, 52f), new Color32(23, 33, 43, 255), mutedText, false);
    }

    private void StyleButton(Button button, string label, Vector2 position, Vector2 size, Color buttonColor, Color textColor, bool isPrimary)
    {
        if (button == null || menuCard == null) return;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.SetParent(menuCard, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = buttonColor;
        }

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.color = textColor;
            buttonText.fontSize = isPrimary ? 20 : 17;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.enableAutoSizing = false;
            buttonText.enableWordWrapping = false;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(190, 255, 220, 255);
        colors.pressedColor = new Color32(160, 210, 185, 255);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color32(100, 100, 100, 120);
        colors.fadeDuration = 0.15f;

        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }
}