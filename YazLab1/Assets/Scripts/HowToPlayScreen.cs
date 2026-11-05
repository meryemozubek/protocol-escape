
using UnityEngine;
using UnityEngine.UI;

// "Nasıl Oynanır" panelinin butonlarını yönetir.
// X ve GERİ DÖN butonlarını görselin üzerine otomatik yerleştirir.
// Bu script HowToPlayPanel nesnesine eklenir.
public class HowToPlayScreen : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Nasıl Oynanır paneli.")]
    [SerializeField] private GameObject howToPlayPanel;

    [Tooltip("PNG görselinin RectTransform bileşeni.")]
    [SerializeField] private RectTransform guideImage;

    [Tooltip("Normalde geri dönülecek panel (MainMenuPanel).")]
    [SerializeField] private GameObject returnPanel;

    [Header("Ayarlar")]
    [SerializeField] private bool fixAspectRatio = true;
    [SerializeField] private bool closeWithEscape = true;

    [Tooltip("Buton alanlarını kırmızı gösterir.")]
    [SerializeField] private bool showDebugAreas = false;

    // Butonların görsel üzerindeki oransal konumları
    private static readonly Rect CloseArea =
        new Rect(0.9387f, 0.8970f, 0.0338f, 0.0475f);

    private static readonly Rect BackArea =
        new Rect(0.4175f, 0.0430f, 0.1649f, 0.0570f);

    private const float ImageAspect = 1536f / 1024f;

    // Ölüm ekranından açılırsa geri dönülecek panel
    private GameObject temporaryReturnPanel;

    private void Awake()
    {
        if (guideImage == null)
        {
            Debug.LogWarning(
                "HowToPlayScreen: Guide Image atanmamış!"
            );
            return;
        }

        if (howToPlayPanel == null)
            howToPlayPanel = gameObject;

        if (fixAspectRatio)
        {
            AspectRatioFitter fitter =
                guideImage.GetComponent<AspectRatioFitter>();

            if (fitter == null)
            {
                fitter = guideImage.gameObject
                    .AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;

            fitter.aspectRatio = ImageAspect;
        }

        CreateHitArea("CloseHitArea", CloseArea);
        CreateHitArea("BackHitArea", BackArea);
    }

    private void Update()
    {
        if (closeWithEscape &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    // Paneli kapatır ve geldiği ekrana döner
    public void Close()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        GameObject target = temporaryReturnPanel != null
            ? temporaryReturnPanel
            : returnPanel;

        temporaryReturnPanel = null;

        if (target != null)
            target.SetActive(true);
    }

    // Ölüm ekranı gibi başka bir panelden açmak için
    public void OpenFrom(GameObject returnTo)
    {
        temporaryReturnPanel = returnTo;

        GameObject panel = howToPlayPanel != null
            ? howToPlayPanel
            : gameObject;

        panel.SetActive(true);

        // Diğer panellerin üstünde görünmesini sağlar
        panel.transform.SetAsLastSibling();
    }

    // PNG üzerindeki şeffaf butonları oluşturur
    private void CreateHitArea(string objectName, Rect area)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        obj.transform.SetParent(guideImage, false);
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
            new Color(1f, 1f, 1f, 0.15f);

        colors.pressedColor =
            new Color(1f, 1f, 1f, 0.30f);

        colors.selectedColor = colors.normalColor;
        colors.fadeDuration = 0.1f;

        button.colors = colors;

        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.None;

        button.navigation = navigation;

        button.onClick.AddListener(Close);
    }
}
