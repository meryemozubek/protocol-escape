
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NPCDialogueUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private OllamaManager ollamaManager;
    // Keep the serialized field for MazeScene and MazeScene_Yedek.
    [SerializeField] private DoorScript doorScript;
    [SerializeField] private FinishTriggerController exitController;
    [SerializeField] private bool usePhysicalExit = false;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("ARGUS Panel Kapatma")]
    [SerializeField] private GameObject argusPanel;
    [SerializeField] private float closeDelay = 2f;

    [Header("UI Elemanları")]
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private TextMeshProUGUI npcResponseText;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Davranış")]
    [SerializeField] private float typewriterCharsPerSecond = 45f;

    private static readonly Color ThinkingColor =
        new Color32(130, 150, 176, 255);

    private static readonly Color GrantedColor =
        new Color32(52, 211, 153, 255);

    private static readonly Color DeniedColor =
        new Color32(248, 113, 113, 255);

    private static readonly Color WarningColor =
        new Color32(251, 191, 36, 255);

    private Color defaultTextColor = Color.white;

    private Coroutine typingRoutine;
    private Coroutine thinkingRoutine;
    private Coroutine closeRoutine;

    private bool isProcessing;
    private bool doorOpened;
    private bool requestPending;
    private int requestVersion;
    private int managerConversationId = -1;

    private void OnEnable()
    {
        requestVersion++;
        requestPending = false;
        EnsureConversation();
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
        }
        if (playerHealth != null)
            playerHealth.OnPlayerDied += HandlePlayerDied;

        if (npcResponseText != null)
        {
            npcResponseText.text = "";
            npcResponseText.maxVisibleCharacters = 99999;
        }
        if (statusText != null)
            statusText.text = "";
        SetProcessing(false);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnPlayerDied -= HandlePlayerDied;
        InvalidateConversation();
    }

    private void HandlePlayerDied()
    {
        InvalidateConversation();
        if (argusPanel != null)
            argusPanel.SetActive(false);
    }

    private void InvalidateConversation()
    {
        requestVersion++;
        requestPending = false;
        if (ollamaManager != null) ollamaManager.EndConversation(managerConversationId);
        managerConversationId = -1;
        StopThinking();
        StopRoutine(ref typingRoutine);
        StopRoutine(ref closeRoutine);
        SetProcessing(false);
    }

    private void EnsureConversation()
    {
        if (ollamaManager != null && !ollamaManager.IsConversationCurrent(managerConversationId))
            managerConversationId = ollamaManager.BeginConversation();
    }

    private bool CanContinueConversation()
    {
        if (!isActiveAndEnabled ||
            (argusPanel != null && !argusPanel.activeInHierarchy))
            return false;

        if (playerHealth != null)
        {
            if (!playerHealth.gameObject.activeInHierarchy || playerHealth.CurrentHealth <= 0)
                return false;
        }
        else if (usePhysicalExit)
        {
            return false;
        }

        return !usePhysicalExit || (exitController != null &&
            (exitController.victoryPanel == null || !exitController.victoryPanel.activeInHierarchy));
    }

    private bool IsRequestValid(int version)
    {
        return this != null && requestPending &&
            requestVersion == version && CanContinueConversation() &&
            ollamaManager != null && ollamaManager.IsConversationCurrent(managerConversationId);
    }

    private void Start()
    {
        if (ollamaManager == null)
        {
            ollamaManager = FindFirstObjectByType<OllamaManager>();
        }
        EnsureConversation();
        ConfigureArgusReadability();

        if (npcResponseText != null)
        {
            defaultTextColor = npcResponseText.color;
            npcResponseText.text = "";
        }

        if (statusText != null)
        {
            statusText.text = "";
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendRequested);
        }

        if (playerInputField != null)
        {
            playerInputField.onSubmit.AddListener(OnInputSubmitted);
        }

        SetProcessing(false);
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(OnSendRequested);
        }

        if (playerInputField != null)
        {
            playerInputField.onSubmit.RemoveListener(OnInputSubmitted);
        }
    }

    private void OnInputSubmitted(string _)
    {
        OnSendRequested();
    }

    private void OnSendRequested()
    {
        if (!isActiveAndEnabled) return;
        if (usePhysicalExit)
            doorOpened = exitController != null && exitController.IsUnlocked;
        if (isProcessing || doorOpened)
        {
            return;
        }

        if (playerInputField == null)
        {
            Debug.LogWarning("ARGUS InputField atanmamış!");
            return;
        }

        string userMessage = playerInputField.text.Trim();

        if (string.IsNullOrEmpty(userMessage))
        {
            ShowStatus("Lütfen bir mesaj yazın.", WarningColor);
            FocusInput();
            return;
        }

        if (ollamaManager == null)
        {
            ShowStatus("Ollama yöneticisi bulunamadı.", WarningColor);
            return;
        }

        if (usePhysicalExit && (playerHealth == null || exitController == null))
        {
            ShowStatus("\u00c7\u0131k\u0131\u015f sistemi haz\u0131r de\u011fil. Tekrar deneyin.", WarningColor);
            return;
        }
        if (!CanContinueConversation()) return;

        EnsureConversation();
        int version = ++requestVersion;
        requestPending = true;
        playerInputField.text = "";

        SetProcessing(true);

        StopRoutine(ref typingRoutine);
        StartThinking();

        // Mevcut sahne referanslarından türetilir; yeni GameObject/Inspector alanı yok.
        FinishTriggerController requestedExit = exitController;
        PlayerHealth requestedPlayer = playerHealth;
        var escapeContext = new ArgusEscapeContext(() =>
            IsRequestValid(version) && usePhysicalExit &&
            requestedExit != null && exitController == requestedExit &&
            requestedPlayer != null && playerHealth == requestedPlayer &&
            !requestedExit.IsUnlocked);

        ollamaManager.AskNPC(
            userMessage,
            data => { if (IsRequestValid(version)) HandleResult(data); },
            message => { if (IsRequestValid(version)) HandleError(message); },
            () => IsRequestValid(version),
            escapeContext
        );
    }

    private void HandleResult(NPCResponseData data)
    {
        if (!requestPending || !CanContinueConversation()) return;
        if (data == null)
        {
            HandleError("ARGUS yanıtı alınamadı. Tekrar deneyin.");
            return;
        }

        StopThinking();

        // Onay ancak fiziksel çıkış da açıldığında arayüze yansıtılır.
        if (data.door_unlocked)
        {
            if (ollamaManager == null || !ollamaManager.TryConsumeExitPermission(data))
            {
                HandleError("ARGUS izni artık geçerli değil. Konuşmayı yeniden başlatın.");
                return;
            }
            if (!TryUnlockDoor())
            {
                HandleError("Çıkış kilidi açılamadı. Sahne bağlantısını kontrol edin.");
                return;
            }
            Debug.Log("[ARGUS] Çıkış kilidi açıldı.");
        }

        if (usePhysicalExit)
            doorOpened = exitController != null && exitController.IsUnlocked;
        bool granted = doorOpened;
        requestPending = false;
        SetProcessing(false);

        ShowStatus(
            granted ? "ERİŞİM ONAYLANDI" : "ERİŞİM REDDEDİLDİ",
            granted ? GrantedColor : DeniedColor
        );

        // ARGUS cevabını yazı animasyonuyla göster
        StopRoutine(ref typingRoutine);

        typingRoutine = StartCoroutine(
            TypeText(data.dialogue)
        );

        // Kapı açıldıysa cevap tamamlanınca paneli kapat
        if (doorOpened && argusPanel != null)
        {
            StopRoutine(ref closeRoutine);

            closeRoutine = StartCoroutine(
                CloseArgusAfterResponse()
            );
        }
    }

    private bool TryUnlockDoor()
    {
        if (!requestPending || !CanContinueConversation()) return false;
        // Physical mode never falls back to a puzzle door or searches the scene.
        if (usePhysicalExit)
            return exitController != null && exitController.UnlockExit();

        if (doorScript == null)
            return false;

        doorScript.OpenDoor();
        doorOpened = true;
        return true;
    }

    private void HandleError(string message)
    {
        if (!requestPending || !CanContinueConversation()) return;
        requestPending = false;
        StopThinking();
        StopRoutine(ref typingRoutine);
        StopRoutine(ref closeRoutine);
        SetProcessing(false);
        if (npcResponseText != null)
            npcResponseText.text = "";
        ShowStatus(message, WarningColor);
    }

    private void StartThinking()
    {
        ShowStatus(
            "Robot yanıt hazırlıyor...",
            ThinkingColor
        );

        StopRoutine(ref thinkingRoutine);

        thinkingRoutine = StartCoroutine(
            ThinkingAnimation()
        );
    }

    private void StopThinking()
    {
        StopRoutine(ref thinkingRoutine);

        if (npcResponseText != null)
        {
            npcResponseText.color = defaultTextColor;
        }
    }

    private IEnumerator ThinkingAnimation()
    {
        if (npcResponseText == null)
        {
            yield break;
        }

        string[] frames =
        {
            "Düşünüyor",
            "Düşünüyor.",
            "Düşünüyor..",
            "Düşünüyor..."
        };

        int index = 0;

        SetResponseTypography(true);
        npcResponseText.maxVisibleCharacters = 99999;
        npcResponseText.color = ThinkingColor;

        while (true)
        {
            npcResponseText.text =
                "S\u0130STEM: " + frames[index % frames.Length];

            index++;

            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    private IEnumerator TypeText(string text)
    {
        if (npcResponseText == null)
        {
            typingRoutine = null;
            yield break;
        }

        SetResponseTypography(false);
        npcResponseText.text = text ?? "";
        npcResponseText.maxVisibleCharacters = 0;
        npcResponseText.ForceMeshUpdate();

        int total = npcResponseText.textInfo.characterCount;

        float elapsed = 0f;
        int visible = 0;

        while (visible < total)
        {
            elapsed += Time.unscaledDeltaTime;

            visible = Mathf.Min(
                total,
                Mathf.FloorToInt(
                    elapsed * typewriterCharsPerSecond
                )
            );

            npcResponseText.maxVisibleCharacters = visible;

            yield return null;
        }

        npcResponseText.maxVisibleCharacters = 99999;

        typingRoutine = null;
    }

    private IEnumerator CloseArgusAfterResponse()
    {
        // ARGUS'un yazı animasyonunu bitirmesini bekle
        while (typingRoutine != null)
        {
            yield return null;
        }

        // Oyuncunun cevabı okuyabilmesi için bekle
        yield return new WaitForSecondsRealtime(closeDelay);

        if (argusPanel != null)
        {
            argusPanel.SetActive(false);

            Debug.Log(
                "Kapı açıldı. ARGUS paneli kapatıldı."
            );
        }

        closeRoutine = null;
    }

    private void ShowStatus(string message, Color color)
    {
        string notification = "S\u0130STEM: " + message;
        if (statusText != null && statusText.isActiveAndEnabled)
        {
            statusText.text = notification;
            statusText.color = color;
        }
        else if (npcResponseText != null)
        {
            SetResponseTypography(true);
            npcResponseText.text = notification;
            npcResponseText.color = color;
            npcResponseText.maxVisibleCharacters = 99999;
        }
    }

    private void SetProcessing(bool processing)
    {
        isProcessing = processing;

        if (sendButton != null)
        {
            sendButton.interactable = !processing && !doorOpened;
        }

        if (playerInputField != null)
        {
            playerInputField.interactable = !processing && !doorOpened;
        }

        if (!processing && isActiveAndEnabled &&
            playerInputField != null &&
            playerInputField.isActiveAndEnabled)
        {
            FocusInput();
        }
    }

    // Style existing ARGUS text/input components only. Never touch the panel Image,
    // sprite, RectTransforms, fonts, other scene objects or the PNG artwork.
    private void ConfigureArgusReadability()
    {
        if (!usePhysicalExit || argusPanel != gameObject) return;
        if (playerInputField != null)
        {
            playerInputField.customCaretColor = true;
            playerInputField.caretColor = new Color32(72, 230, 236, 255);
            playerInputField.caretWidth = 2;
            playerInputField.caretBlinkRate = 0.8f;
            playerInputField.selectionColor = new Color(0.18f, 0.83f, 0.9f, 0.35f);
            playerInputField.onFocusSelectAll = false;
            playerInputField.richText = false;
            playerInputField.pointSize = 24f;

            if (playerInputField.textComponent != null)
            {
                TMP_Text text = playerInputField.textComponent;
                text.color = new Color32(235, 245, 250, 255);
                text.richText = false;
                text.extraPadding = true;
                text.margin = new Vector4(8f, 4f, 8f, 4f);
            }
            TMP_Text placeholder = playerInputField.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.fontSize = 21f;
                placeholder.color = new Color32(135, 167, 184, 200);
                placeholder.fontStyle = FontStyles.Italic;
            }
            // Tint only the existing input graphic; the parent's PNG stays intact.
            if (playerInputField.targetGraphic != null &&
                playerInputField.targetGraphic.gameObject == playerInputField.gameObject)
            {
                playerInputField.targetGraphic.color = Color.white;
                ColorBlock colors = playerInputField.colors;
                colors.normalColor = new Color(0.12f, 0.35f, 0.42f, 0.06f);
                colors.highlightedColor = new Color(0.2f, 0.85f, 0.92f, 0.12f);
                colors.selectedColor = new Color(0.2f, 0.85f, 0.92f, 0.24f);
                colors.pressedColor = colors.selectedColor;
                colors.disabledColor = new Color(0.12f, 0.35f, 0.42f, 0.03f);
                colors.fadeDuration = 0.12f;
                playerInputField.transition = Selectable.Transition.ColorTint;
                playerInputField.colors = colors;
            }
        }
        SetResponseTypography(false);
        if (npcResponseText != null)
        {
            npcResponseText.color = new Color32(224, 243, 248, 255);
            npcResponseText.richText = false;
            npcResponseText.extraPadding = true;
            npcResponseText.raycastTarget = false;
            npcResponseText.margin = new Vector4(18f, 18f, 18f, 18f);
            npcResponseText.alignment = TextAlignmentOptions.TopLeft;
            npcResponseText.lineSpacing = 5f;
            npcResponseText.paragraphSpacing = 8f;
        }
    }

    private void SetResponseTypography(bool systemNotification)
    {
        if (!usePhysicalExit || argusPanel != gameObject || npcResponseText == null) return;
        npcResponseText.enableWordWrapping = true;
        npcResponseText.enableAutoSizing = true;
        npcResponseText.fontSizeMin = systemNotification ? 16f : 20f;
        npcResponseText.fontSizeMax = systemNotification ? 20f : 28f;
        npcResponseText.fontSize = npcResponseText.fontSizeMax;
        // Autosizing plus the existing <=200-character validator bounds the text.
        npcResponseText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void FocusInput()
    {
        if (doorOpened || !CanContinueConversation() || playerInputField == null ||
            !playerInputField.isActiveAndEnabled || !playerInputField.interactable) return;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(playerInputField.gameObject);
        playerInputField.ActivateInputField();
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}

