
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    private int conversationVersion;

    private void OnEnable()
    {
        conversationVersion++;
        requestPending = false;
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
        conversationVersion++;
        requestPending = false;
        StopThinking();
        StopRoutine(ref typingRoutine);
        StopRoutine(ref closeRoutine);
        SetProcessing(false);
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
            conversationVersion == version && CanContinueConversation();
    }

    private void Start()
    {
        if (ollamaManager == null)
        {
            ollamaManager = FindFirstObjectByType<OllamaManager>();
        }

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
            playerInputField.ActivateInputField();
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

        int version = ++conversationVersion;
        requestPending = true;
        playerInputField.text = "";

        SetProcessing(true);

        StopRoutine(ref typingRoutine);
        StartThinking();

        ollamaManager.AskNPC(
            userMessage,
            data => { if (IsRequestValid(version)) HandleResult(data); },
            message => { if (IsRequestValid(version)) HandleError(message); },
            () => IsRequestValid(version)
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
        SetProcessing(false);

        // Onay ancak fiziksel çıkış da açıldığında arayüze yansıtılır.
        if (data.door_unlocked)
        {
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
            sendButton.interactable = !processing;
        }

        if (playerInputField != null)
        {
            playerInputField.interactable = !processing;
        }

        if (!processing && isActiveAndEnabled &&
            playerInputField != null &&
            playerInputField.isActiveAndEnabled)
        {
            playerInputField.ActivateInputField();
        }
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

