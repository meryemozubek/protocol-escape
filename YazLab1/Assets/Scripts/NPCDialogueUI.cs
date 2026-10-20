using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCDialogueUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private OllamaManager ollamaManager;
    [SerializeField] private DoorScript doorScript; // Sahnedeki kapı script'i

    [Header("UI Elemanları")]
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private TextMeshProUGUI npcResponseText;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI statusText; 

    private void Start()
    {
        // Butona basıldığında Gönder 
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        if (ollamaManager == null)
        {
            ollamaManager = FindObjectOfType<OllamaManager>();
        }

        SetUIState(isProcessing: false);
    }

    private void OnSendButtonClicked()
    {
        string userMessage = playerInputField.text.Trim();

        if (string.IsNullOrEmpty(userMessage))
        {
            if (statusText != null) statusText.text = "Lütfen bir mesaj yazın!";
            return;
        }

        // İstek atılırken UI'ı kilitle
        SetUIState(isProcessing: true);
        if (statusText != null) statusText.text = "Robot düşünüyor...";

        // OllamaManager üzerinden LLM'e istek gönder
        ollamaManager.AskNPC(
            userMessage,
            onResult: (npcData) =>
            {
                // Başarılı yanıt
                SetUIState(isProcessing: false);
                if (statusText != null) statusText.text = "";

                npcResponseText.text = npcData.dialogue;

             
                if (npcData.door_unlocked)
                {
                    if (doorScript != null)
                    {
                        doorScript.OpenDoor(); // Kapıyı açan metot
                    }
                    Debug.Log("Kapı kilidi açıldı!");
                }
            },
            onError: (errorMessage) =>
            {
             
                SetUIState(isProcessing: false);
                if (statusText != null) statusText.text = errorMessage;
            }
        );

      
        playerInputField.text = "";
    }

   
    private void SetUIState(bool isProcessing)
    {
        if (sendButton != null)
        {
            sendButton.interactable = !isProcessing;
        }
    }
}