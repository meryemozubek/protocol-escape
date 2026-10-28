using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUILayout : MonoBehaviour
{
    [Header("UI Elemanları")]
    public RectTransform dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TMP_InputField inputField;
    public Button sendButton;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI statusText;

    [Header("Panel Ayarları")]
    public float panelWidth = 900f;
    public float panelHeight = 250f;
    public float bottomOffset = 24f;
    public float animationDuration = 0.25f;

    [Header("Görünüm")]
    public string headerTitle = "GÜVENLİK TERMİNALİ";
    public bool hidePanelOnStart = true;
    [Range(0.3f, 1.5f)]
    [Tooltip("Köşe yuvarlaklığı. Küçük değer = daha yuvarlak köşe.")]
    public float cornerScale = 0.6f;

    // Renk paleti
    private static readonly Color32 PanelColor  = new Color32(11, 18, 32, 245);
    private static readonly Color32 CardColor   = new Color32(22, 35, 56, 255);
    private static readonly Color32 AccentColor = new Color32(45, 212, 191, 255);
    private static readonly Color32 MainText    = new Color32(232, 240, 250, 255);
    private static readonly Color32 MutedText   = new Color32(130, 150, 176, 255);
    private static readonly Color32 InputColor  = new Color32(24, 38, 60, 255);
    private static readonly Color32 ButtonColor = new Color32(14, 116, 200, 255);

    private const string HeaderName = "TerminalHeader";
    private const string HeaderLineName = "HeaderLine";
    private const string CardName = "RobotMessageCard";
    private const string AccentBarName = "AccentBar";

    private Coroutine animationRoutine;

    private void Start()
    {
        if (!Application.isPlaying) return;

        ApplyLayout();
        if (hidePanelOnStart && dialoguePanel != null) dialoguePanel.gameObject.SetActive(false);
    }

    [ContextMenu("Modern Tasarımı Uygula")]
    public void ApplyLayout()
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("DialogueUILayout: Dialogue Panel atanmamış!");
            return;
        }

        StylePanel();
        StyleHeader();
        StyleMessageCard();
        StyleStatus();
        StyleInput();
        StyleButton();
        StyleHealth();
    }

    // ---------------- Bölümler ----------------

    private void StylePanel()
    {
        dialoguePanel.anchorMin = new Vector2(0.5f, 0f);
        dialoguePanel.anchorMax = new Vector2(0.5f, 0f);
        dialoguePanel.pivot = new Vector2(0.5f, 0f);
        dialoguePanel.anchoredPosition = new Vector2(0f, bottomOffset);
        dialoguePanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image image = dialoguePanel.GetComponent<Image>();
        if (image != null)
        {
            image.color = PanelColor;
            image.raycastTarget = true;
            ApplyRoundedSprite(image);
        }

        Outline outline = GetOrAdd<Outline>(dialoguePanel.gameObject);
        outline.effectColor = new Color32(45, 180, 200, 120);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void StyleHeader()
    {
        TextMeshProUGUI header = GetOrCreateText(HeaderName, dialoguePanel);
        header.text = headerTitle;
        header.fontSize = 15;
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 4f;
        header.color = AccentColor;
        header.alignment = TextAlignmentOptions.MidlineLeft;
        header.raycastTarget = false;

        RectTransform rect = header.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.8f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(24f, 0f);
        rect.offsetMax = new Vector2(-24f, -6f);

    
        Image line = GetOrCreateImage(HeaderLineName, dialoguePanel);
        line.color = new Color32(45, 212, 191, 70);
        line.raycastTarget = false;

        RectTransform lineRect = line.rectTransform;
        lineRect.anchorMin = new Vector2(0f, 0.8f);
        lineRect.anchorMax = new Vector2(1f, 0.8f);
        lineRect.offsetMin = new Vector2(24f, 0f);
        lineRect.offsetMax = new Vector2(-24f, 1f);
    }

    private void StyleMessageCard()
    {
        Image cardImage = GetOrCreateImage(CardName, dialoguePanel);
        cardImage.color = CardColor;
        cardImage.raycastTarget = false;
        ApplyRoundedSprite(cardImage);

        RectTransform card = cardImage.rectTransform;
        card.anchorMin = new Vector2(0f, 0.33f);
        card.anchorMax = new Vector2(1f, 0.77f);
        card.offsetMin = new Vector2(22f, 0f);
        card.offsetMax = new Vector2(-22f, 0f);

        // Sol vurgu çubuğu
        Image bar = GetOrCreateImage(AccentBarName, card);
        bar.color = AccentColor;
        bar.raycastTarget = false;

        RectTransform barRect = bar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0.12f);
        barRect.anchorMax = new Vector2(0f, 0.88f);
        barRect.offsetMin = new Vector2(10f, 0f);
        barRect.offsetMax = new Vector2(14f, 0f);

        if (dialogueText == null) return;

        dialogueText.transform.SetParent(card, false);

        RectTransform textRect = dialogueText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        dialogueText.fontSize = 18;
        dialogueText.lineSpacing = 6f;
        dialogueText.color = MainText;
        dialogueText.alignment = TextAlignmentOptions.MidlineLeft;
        dialogueText.enableWordWrapping = true;
        dialogueText.overflowMode = TextOverflowModes.Ellipsis;
        dialogueText.raycastTarget = false;
    }

    private void StyleStatus()
    {
        if (statusText == null) return;

        statusText.transform.SetParent(dialoguePanel, false);

        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.245f);
        rect.anchorMax = new Vector2(1f, 0.33f);
        rect.offsetMin = new Vector2(26f, 0f);
        rect.offsetMax = new Vector2(-24f, 0f);

        statusText.fontSize = 12;
        statusText.fontStyle = FontStyles.Bold;
        statusText.characterSpacing = 2f;
        statusText.color = MutedText;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;
        statusText.raycastTarget = false;
    }

    private void StyleInput()
    {
        if (inputField == null) return;

        RectTransform rect = inputField.GetComponent<RectTransform>();
        rect.SetParent(dialoguePanel, false);
        rect.anchorMin = new Vector2(0f, 0.06f);
        rect.anchorMax = new Vector2(0.77f, 0.235f);
        rect.offsetMin = new Vector2(24f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);

        Image image = inputField.GetComponent<Image>();
        if (image != null)
        {
            image.color = InputColor;
            ApplyRoundedSprite(image);

            Outline outline = GetOrAdd<Outline>(inputField.gameObject);
            outline.effectColor = new Color32(45, 75, 105, 180);
            outline.effectDistance = new Vector2(1f, -1f);

            inputField.targetGraphic = image;
        }

        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.customCaretColor = true;
        inputField.caretColor = AccentColor;
        inputField.caretWidth = 2;
        inputField.selectionColor = new Color32(45, 212, 191, 90);

        if (inputField.textComponent != null)
        {
            inputField.textComponent.color = MainText;
            inputField.textComponent.fontSize = 15;
            inputField.textComponent.margin = new Vector4(12f, 7f, 12f, 7f);
        }

        if (inputField.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.text = "Mesajınızı buraya yazın ve Enter'a basın...";
            placeholder.fontSize = 14;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = MutedText;
            placeholder.margin = new Vector4(12f, 7f, 12f, 7f);
        }
    }

    private void StyleButton()
    {
        if (sendButton == null) return;

        RectTransform rect = sendButton.GetComponent<RectTransform>();
        rect.SetParent(dialoguePanel, false);
        rect.anchorMin = new Vector2(0.79f, 0.06f);
        rect.anchorMax = new Vector2(1f, 0.235f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(-24f, 0f);

        Image image = sendButton.GetComponent<Image>();
        if (image != null) image.color = ButtonColor;

        ColorBlock colors = sendButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(210, 245, 255, 255);
        colors.pressedColor = new Color32(150, 210, 245, 255);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color32(120, 130, 145, 160);
        sendButton.colors = colors;

        TextMeshProUGUI label = sendButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = "GÖNDER";
            label.fontSize = 14;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 2f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }

    private void StyleHealth()
    {
        if (healthText == null) return;

        RectTransform rect = healthText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(32f, -24f);

        healthText.fontSize = 35;
        healthText.fontStyle = FontStyles.Bold;
        healthText.color = new Color32(255, 255, 255, 255);
    }

    // ---------------- Aç / Kapat  ----------------

    public void ShowDialoguePanel()
    {
        if (dialoguePanel == null) return;

        dialoguePanel.gameObject.SetActive(true);
        ApplyLayout();
        StartAnimation(show: true);
    }

    public void HideDialoguePanel()
    {
        if (dialoguePanel == null || !dialoguePanel.gameObject.activeSelf) return;

        if (!isActiveAndEnabled)
        {
            dialoguePanel.gameObject.SetActive(false);
            return;
        }
        StartAnimation(show: false);
    }

    private void StartAnimation(bool show)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimatePanel(show));
    }

    private IEnumerator AnimatePanel(bool show)
    {
        CanvasGroup group = GetOrAdd<CanvasGroup>(dialoguePanel.gameObject);

        Vector2 shownPos = new Vector2(0f, bottomOffset);
        Vector2 hiddenPos = new Vector2(0f, bottomOffset - 30f);

        float fromAlpha = show ? 0f : group.alpha;
        float toAlpha = show ? 1f : 0f;
        Vector2 fromPos = show ? hiddenPos : dialoguePanel.anchoredPosition;
        Vector2 toPos = show ? shownPos : hiddenPos;

        group.interactable = show;
        group.blocksRaycasts = show;

        float t = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            dialoguePanel.anchoredPosition = Vector2.Lerp(fromPos, toPos, eased);
            yield return null;
        }

        group.alpha = toAlpha;
        dialoguePanel.anchoredPosition = toPos;
        animationRoutine = null;

        if (show)
        {
            if (inputField != null) inputField.ActivateInputField();
        }
        else
        {
            dialoguePanel.gameObject.SetActive(false);
        }
    }

    // ---------------- Yardımcılar ----------------

    // Butonun varsayılan yuvarlak köşeli sprite'ını diğer elemanlarda da kullanır
    private void ApplyRoundedSprite(Image target)
    {
        if (target == null || sendButton == null) return;

        Image reference = sendButton.GetComponent<Image>();
        if (reference == null || reference.sprite == null || reference == target) return;

        target.sprite = reference.sprite;
        target.type = Image.Type.Sliced;
        target.pixelsPerUnitMultiplier = cornerScale;
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private TextMeshProUGUI GetOrCreateText(string objectName, RectTransform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            TextMeshProUGUI text = existing.GetComponent<TextMeshProUGUI>();
            if (text != null) return text;
        }

        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<TextMeshProUGUI>();
    }

    private Image GetOrCreateImage(string objectName, RectTransform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            Image image = existing.GetComponent<Image>();
            if (image != null) return image;
        }

        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<Image>();
    }
}