using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class NPCDialogueUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private OllamaManager ollamaManager;
    [SerializeField] private DoorScript doorScript;

    [Header("UI Elemanları")]
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private TextMeshProUGUI npcResponseText;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Davranış")]
    [SerializeField] private float typewriterCharsPerSecond = 45f;
    [SerializeField, TextArea(2, 3)]
    private string welcomeMessage = "Güvenlik robotu iletişim için hazır. Lütfen kimliğinizi belirtin.";

    private static readonly Color ThinkingColor = new Color32(130, 150, 176, 255);
    private static readonly Color GrantedColor  = new Color32(52, 211, 153, 255);
    private static readonly Color DeniedColor   = new Color32(248, 113, 113, 255);
    private static readonly Color WarningColor  = new Color32(251, 191, 36, 255);

    private Color defaultTextColor = Color.white;
    private Coroutine typingRoutine;
    private Coroutine thinkingRoutine;
    private bool isProcessing;

    private void Start()
    {
        if (ollamaManager == null)
        {
            ollamaManager = FindFirstObjectByType<OllamaManager>();
        }

        if (npcResponseText != null)
        {
            defaultTextColor = npcResponseText.color;
            npcResponseText.text = welcomeMessage;
        }

        if (statusText != null) statusText.text = "";

        if (sendButton != null) sendButton.onClick.AddListener(OnSendRequested);
        if (playerInputField != null) playerInputField.onSubmit.AddListener(OnInputSubmitted);

        SetProcessing(false);
    }

    private void OnDestroy()
    {
        if (sendButton != null) sendButton.onClick.RemoveListener(OnSendRequested);
        if (playerInputField != null) playerInputField.onSubmit.RemoveListener(OnInputSubmitted);
    }

    private void OnInputSubmitted(string _) => OnSendRequested();

    private void OnSendRequested()
    {
        if (isProcessing) return;

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

        playerInputField.text = "";
        SetProcessing(true);
        StopRoutine(ref typingRoutine);
        StartThinking();

        ollamaManager.AskNPC(userMessage, HandleResult, HandleError);
    }

    private void HandleResult(NPCResponseData data)
    {
        StopThinking();
        SetProcessing(false);

        bool granted = data.door_unlocked;
        ShowStatus(granted ? "ERİŞİM ONAYLANDI" : "ERİŞİM REDDEDİLDİ",
                   granted ? GrantedColor : DeniedColor);


        if (granted && doorScript != null)
        {
            doorScript.OpenDoor();
            Debug.Log("Kapı kilidi açıldı!");
        }

        StopRoutine(ref typingRoutine);
        typingRoutine = StartCoroutine(TypeText(data.dialogue));
    }

    private void HandleError(string message)
    {
        StopThinking();
        SetProcessing(false);

        ShowStatus(message, WarningColor);

        if (npcResponseText != null)
        {
            npcResponseText.color = defaultTextColor;
            npcResponseText.maxVisibleCharacters = 99999;
            npcResponseText.text = "NPC şu anda yanıt veremiyor.";
        }
    }



    private void StartThinking()
    {
        ShowStatus("Robot yanıt hazırlıyor...", ThinkingColor);
        StopRoutine(ref thinkingRoutine);
        thinkingRoutine = StartCoroutine(ThinkingAnimation());
    }

    private void StopThinking()
    {
        StopRoutine(ref thinkingRoutine);
        if (npcResponseText != null) npcResponseText.color = defaultTextColor;
    }

    private IEnumerator ThinkingAnimation()
    {
        if (npcResponseText == null) yield break;

        string[] frames = { "Düşünüyor", "Düşünüyor.", "Düşünüyor..", "Düşünüyor..." };
        int index = 0;

        npcResponseText.maxVisibleCharacters = 99999;
        npcResponseText.color = ThinkingColor;

        while (true)
        {
            npcResponseText.text = frames[index % frames.Length];
            index++;
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    private IEnumerator TypeText(string text)
    {
        if (npcResponseText == null) yield break;

        npcResponseText.text = text;
        npcResponseText.maxVisibleCharacters = 0;
        npcResponseText.ForceMeshUpdate();

        int total = npcResponseText.textInfo.characterCount;
        float elapsed = 0f;
        int visible = 0;

        while (visible < total)
        {
            elapsed += Time.unscaledDeltaTime;
            visible = Mathf.Min(total, Mathf.FloorToInt(elapsed * typewriterCharsPerSecond));
            npcResponseText.maxVisibleCharacters = visible;
            yield return null;
        }

        npcResponseText.maxVisibleCharacters = 99999;
        typingRoutine = null;
    }


    private void ShowStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
    }

    private void SetProcessing(bool processing)
    {
        isProcessing = processing;

        if (sendButton != null) sendButton.interactable = !processing;
        if (playerInputField != null) playerInputField.interactable = !processing;

        if (!processing && playerInputField != null && playerInputField.isActiveAndEnabled)
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