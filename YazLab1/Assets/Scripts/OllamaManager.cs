using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// LLM'den gelen yapılandırılmış JSON verisini karşılayan model
[Serializable]
public class NPCResponseData
{
    public string dialogue;
    public bool door_unlocked;
}

// Ollama API'ye giden istek modeli
[Serializable]
public class OllamaRequest
{
    public string model;
    public string prompt;
    public bool stream = false;
}

// Ollama API'den dönen yanıt modeli
[Serializable]
public class OllamaResponse
{
    public string response;
    public bool done;
}

// HTTP haberleşme servisi
public class OllamaApiClient
{
    private readonly string apiUrl;
    private readonly int timeoutSeconds;

    public OllamaApiClient(string url, int timeout = 15)
    {
        apiUrl = url;
        timeoutSeconds = timeout;
    }

    //POST isteği atan coroutine
    public IEnumerator PostRequest(string jsonPayload, Action<string> onSuccess, Action<string> onError)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.timeout = timeoutSeconds;

            yield return webRequest.SendWebRequest();

            // Sunucu veya bağlantı hatası kontrolü
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(webRequest.error);
            }
            else
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }
}

// Ana Ollama Yöneticisi
public class OllamaManager : MonoBehaviour
{
    private const string OLLAMA_URL = "http://localhost:11434/api/generate";

    [Header("Ollama Model Ayarları")]
    [SerializeField] private string modelName = "llama3.2:1b";

    [TextArea(5, 10)]
    [SerializeField]
    private string systemPrompt = 
        "Sen kilitli bir kapının koruyucusu olan aksi bir robot NPC'sin. " +
        "Oyuncunun sorularına Türkçe cevap ver. " +
        "Sadece ve sadece geçerli bir JSON objesi döndür! Başka hiçbir açıklama veya ek metin yazma. " +
        "JSON Şeması: {\"dialogue\": \"oyuncuya vereceğin cevap\", \"door_unlocked\": true/false}. " +
        "Eğer oyuncu seni ikna ederse veya doğru şifreyi söylerse door_unlocked değerini true yap, aksi halde false yap.";

    private OllamaApiClient apiClient;

    private void Awake()
    {
        // HTTP istemcisini başlatıyor
        apiClient = new OllamaApiClient(OLLAMA_URL, 15);
    }

    // Dışarıdan çağrılan ana diyalog fonksiyonu
    public void AskNPC(string playerMessage, Action<NPCResponseData> onResult, Action<string> onError)
    {
        StartCoroutine(SendOllamaRequest(playerMessage, onResult, onError));
    }

    private IEnumerator SendOllamaRequest(string playerMessage, Action<NPCResponseData> onResult, Action<string> onError)
    {
        string fullPrompt = $"{systemPrompt}\n\nOyuncunun Mesajı: \"{playerMessage}\"\nCevap (JSON):";

        OllamaRequest requestData = new OllamaRequest
        {
            model = modelName,
            prompt = fullPrompt,
            stream = false
        };

        string jsonPayload = JsonUtility.ToJson(requestData);

        // HTTP isteğini çalıştırıyoruz
        yield return apiClient.PostRequest(
            jsonPayload,
            onSuccess: (rawTextResponse) =>
            {
                // Gelen yanıtı JSON olarak ayrıştırıyoruz
                try
                {
                    OllamaResponse rawResponse = JsonUtility.FromJson<OllamaResponse>(rawTextResponse);
                    
                    // LLM'in ürettiği metni C# çeviriyoruz
                    NPCResponseData npcData = JsonUtility.FromJson<NPCResponseData>(rawResponse.response.Trim());

                    if (npcData != null && !string.IsNullOrEmpty(npcData.dialogue))
                    {
                        onResult?.Invoke(npcData);
                    }
                    else
                    {
                        throw new Exception("Bozuk JSON verisi geldi.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[JSON Parse Hatasi]: {ex.Message}");
                    onError?.Invoke("NPC anlaşılamayan bir yanıt verdi, tekrar deneyin.");
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"[Ollama Sunucu Hatasi]: {error}");
                onError?.Invoke("Ollama sunucusuna ulaşılamadı. Sunucunun açık olduğundan emin olun.");
            }
        );
    }
}