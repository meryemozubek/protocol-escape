using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaManager : MonoBehaviour
{
    // localhostta çalışan Ollama HTTP sunucu adresi
    private const string OLLAMA_URL = "http://localhost:11434/api/generate";

    [Header("Ollama Model Ayarları")]
    [SerializeField] private string modelName = "llama3.2:1b";

    /// <summary>
    /// UnityWebRequest kullanarak Ollama HTTP sunucusuna POST isteği gönderir.
    /// </summary>
    public void SendRawHttpRequest(string jsonPayload, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostRequestCoroutine(jsonPayload, onSuccess, onError));
    }

    private IEnumerator PostRequestCoroutine(string jsonPayload, Action<string> onSuccess, Action<string> onError)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest webRequest = new UnityWebRequest(OLLAMA_URL, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(webRequest.error);
            }
        }
    }
    
}