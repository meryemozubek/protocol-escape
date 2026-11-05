
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private RectTransform deathImage;
    [SerializeField] private HowToPlayScreen howToPlayScreen;

    [Header("Ayarlar")]
    [SerializeField] private bool fixAspectRatio = true;
    [SerializeField] private bool pauseOnShow = true;
    [SerializeField] private bool showDebugAreas = false;
    [SerializeField] private KeyCode editorTestKey = KeyCode.F3;

    private const float ImageAspect = 1674f / 940f;

    // PNG üzerindeki buton alanları
    private static readonly Rect RetryArea =
        new Rect(0.416f, 0.371f, 0.344f, 0.084f);

    private static readonly Rect MenuArea =
        new Rect(0.416f, 0.263f, 0.344f, 0.082f);

    private static readonly Rect HowToArea =
        new Rect(0.416f, 0.154f, 0.344f, 0.084f);

    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (deathPanel == null || deathImage == null)
        {
            Debug.LogWarning(
                "DeathScreen: DeathPanel veya DeathImage atanmamış!"
            );
            return;
        }

        if (fixAspectRatio)
        {
            AspectRatioFitter fitter =
                deathImage.GetComponent<AspectRatioFitter>();

            if (fitter == null)
            {
                fitter = deathImage.gameObject
                    .AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;

            fitter.aspectRatio = ImageAspect;
        }

        CreateHitArea("RetryHitArea", RetryArea, Retry);
        CreateHitArea("MenuHitArea", MenuArea, GoToMainMenu);
        CreateHitArea("HowToHitArea", HowToArea, OpenHowToPlay);
    }

    // PlayerHealth ölüm olayına abone olur
    private void OnEnable()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied += Show;
        }
        else
        {
            Debug.LogWarning(
                "DeathScreen: PlayerHealth bulunamadı!"
            );
        }
    }

    // Script kapanırsa olay aboneliğini kaldırır
    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied -= Show;
        }
    }

    private void Start()
    {
        // Oyun başında ölüm ekranını gizle
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(editorTestKey))
        {
            Show();
        }
#endif
    }

    // Oyuncunun canı sıfırlandığında çağrılır
    public void Show()
    {
        if (deathPanel == null)
            return;

        deathPanel.SetActive(true);

        // Ölüm ekranını diğer panellerin üstüne getir
        deathPanel.transform.SetAsLastSibling();

        if (pauseOnShow)
        {
            Time.timeScale = 0f;
        }

        Debug.Log("DeathScreen: Ölüm ekranı açıldı.");
    }

    // TEKRAR DENE
    public void Retry()
    {
        Time.timeScale = 1f;

        GameSession.SkipMenuOnNextLoad = true;

        ReloadScene();
    }

    // ANA MENÜ
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        GameSession.SkipMenuOnNextLoad = false;

        ReloadScene();
    }

    // NASIL OYNANIR?
    public void OpenHowToPlay()
    {
        if (howToPlayScreen == null)
        {
            Debug.LogWarning(
                "DeathScreen: HowToPlayScreen atanmamış!"
            );
            return;
        }

        deathPanel.SetActive(false);

        howToPlayScreen.OpenFrom(deathPanel);
    }

    // Mevcut sahneyi yeniden yükle
    private void ReloadScene()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    // PNG üzerine şeffaf buton oluştur
    private void CreateHitArea(
        string objectName,
        Rect area,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        obj.transform.SetParent(deathImage, false);
        obj.transform.SetAsLastSibling();

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(
            area.xMin,
            area.yMin
        );

        rect.anchorMax = new Vector2(
            area.xMax,
            area.yMax
        );

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = obj.GetComponent<Image>();

        image.color = Color.white;
        image.raycastTarget = true;

        Button button = obj.GetComponent<Button>();

        button.targetGraphic = image;
        button.transition =
            Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;

        colors.normalColor = showDebugAreas
            ? new Color(1f, 0f, 0f, 0.35f)
            : new Color(1f, 1f, 1f, 0f);

        colors.highlightedColor =
            new Color(1f, 1f, 1f, 0.12f);

        colors.pressedColor =
            new Color(1f, 1f, 1f, 0.25f);

        colors.selectedColor = colors.normalColor;
        colors.fadeDuration = 0.1f;

        button.colors = colors;

        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.None;

        button.navigation = navigation;

        button.onClick.AddListener(onClick);
    }
}
