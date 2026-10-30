
using System;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

// LLM sunucusuyla haberleşme sözleşmesi.
// OllamaManager yalnızca bu arayüze bağlıdır; sunucu değişirse yönetici değişmez (DIP).
public interface ILLMClient
{
    IEnumerator SendChat(
        string jsonPayload,
        Action<string> onSuccess,
        Action<string> onError
    );
}

// Ollama için HTTP uygulaması (yerel sunucu, bulut bağımlılığı yok)
public class OllamaApiClient : ILLMClient
{
    private readonly string apiUrl;
    private readonly int timeoutSeconds;

    public OllamaApiClient(string url, int timeout = 60)
    {
        apiUrl = url;
        timeoutSeconds = timeout;
    }

    // Asenkron: ana oyun akışını (frame rate) bloklamaz
    public IEnumerator SendChat(
        string jsonPayload,
        Action<string> onSuccess,
        Action<string> onError
    )
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.timeout = timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorDetails = request.downloadHandler != null
                    ? request.downloadHandler.text
                    : "";

                onError?.Invoke(
                    request.error + " | Ollama Detay: " + errorDetails
                );
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
        }
    }
}
