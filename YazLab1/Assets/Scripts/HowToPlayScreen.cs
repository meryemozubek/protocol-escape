using UnityEngine;
using UnityEngine.UI;

// "Nasıl Oynanır" panelinin kapatma butonlarını yönetir.
// Görsel tek bir PNG olduğu için, görselin üzerindeki "X" ve "GERİ DÖN" alanlarına
// çalışma anında butonlar yerleştirilir. Elle konumlandırma ve OnClick bağlama gerekmez.
//
// Bu script HowToPlayPanel objesinin KENDİSİNE eklenir.
public class HowToPlayScreen : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Kapatılacak panel (HowToPlayPanel).")]
    [SerializeField] private GameObject howToPlayPanel;

    [Tooltip("PNG'yi gösteren Image'ın RectTransform'u (TutorialBackground).")]
    [SerializeField] private RectTransform guideImage;

    [Tooltip("Kapatınca geri dönülecek panel (MainMenuPanel). Boş bırakılabilir.")]
    [SerializeField] private GameObject returnPanel;

    [Header("Ayarlar")]
    [SerializeField] private bool fixAspectRatio = true;
    [SerializeField] private bool closeWithEscape = true;

    [Tooltip("Açıkken buton alanları yarı saydam kırmızı görünür (konum kontrolü için).")]
    [SerializeField] private bool showDebugAreas = false;

    // Görsel üzerindeki buton alanları (0-1 arası, sol-alt köşe referanslı)
    private static readonly Rect CloseArea = new Rect(0.9387f, 0.8970f, 0.0338f, 0.0475f);
    private static readonly Rect BackArea  = new Rect(0.4175f, 0.0430f, 0.1649f, 0.0570f);

    private const float ImageAspect = 1536f / 1024f;

    private void Awake()
    {
        if (guideImage == null)
        {
            Debug.LogWarning("HowToPlayScreen: Guide Image atanmamış!");
            return;
        }

        if (howToPlayPanel == null) howToPlayPanel = gameObject;

        if (fixAspectRatio)
        {
            // Görsel oranı bozulmadan paneli doldursun; buton alanları da tam üstüne otursun
            AspectRatioFitter fitter = guideImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = guideImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = ImageAspect;
        }

        CreateHitArea("CloseHitArea", CloseArea);
        CreateHitArea("BackHitArea", BackArea);
    }

    private void Update()
    {
        if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Close()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (returnPanel != null) returnPanel.SetActive(true);
    }

    private void CreateHitArea(string objectName, Rect area)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(guideImage, false);
        obj.transform.SetAsLastSibling(); // diğer elemanların üstünde kalsın

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(area.xMin, area.yMin);
        rect.anchorMax = new Vector2(area.xMax, area.yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor      = showDebugAreas ? new Color(1f, 0f, 0f, 0.35f) : new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.15f); // üzerine gelince hafif parlar
        colors.pressedColor     = new Color(1f, 1f, 1f, 0.30f);
        colors.selectedColor    = colors.normalColor;
        colors.fadeDuration     = 0.1f;
        button.colors = colors;

        // Enter tuşu diyalog kutusundayken bu butonlara odaklanmasın
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.AddListener(Close);
    }
}