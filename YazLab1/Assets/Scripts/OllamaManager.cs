using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// ---------------------------------------------------------------
// KULLANILAN SINIFLAR
// ---------------------------------------------------------------

// İlk aşamada modelin kullanıcının ne demek istediğini anlaması için kullanılıyor.
[Serializable]
public class IntentResult
{
    public string niyet; // Kullanıcının niyetini tutuyor.
    public string rol;   // Kullanıcının söylediği mesleği tutuyor.
}

// İkinci aşamada NPC'nin vereceği cevabı tutuyor.
[Serializable]
public class DialogueResult
{
    public string dialogue;
}

// NPC'den gelen sonucun tutulduğu sınıf.
[Serializable]
public class NPCResponseData
{
    public string niyet;
    public string dialogue;

    // Niyet yetkili ise kapı açılıyor.
    public bool door_unlocked => niyet == "yetkili";
}

[Serializable]
public class RequestOptions
{
    public float temperature = 0.2f;
    public int num_predict = 100;
    public int seed = 42;
}

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class ChatRequest
{
    public string model;
    public ChatMessage[] messages;
    public bool stream = false;
    public RequestOptions options = new RequestOptions();
}

[Serializable]
public class ChatResponseMessage
{
    public string role;
    public string content;
}

[Serializable]
public class ChatResponse
{
    public ChatResponseMessage message;
    public bool done;
}

// ---------------------------------------------------------------
// OLLAMA İLE BAĞLANTI KURULAN KISIM
// ---------------------------------------------------------------

public class OllamaApiClient
{
    private readonly string apiUrl;
    private readonly int timeoutSeconds;

    public OllamaApiClient(string url, int timeout = 30)
    {
        apiUrl = url;
        timeoutSeconds = timeout;
    }

    // Ollama'ya istek gönderip gelen cevabı kontrol ediyoruz.
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

            // İstek başarısız olursa hata mesajını gönderiyoruz.
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(webRequest.error);
            }
            else
            {
                // İstek başarılı olursa gelen cevabı alıyoruz.
                onSuccess?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }
}

// ---------------------------------------------------------------
// GELEN CEVAPLARI KONTROL EDEN KISIM
// ---------------------------------------------------------------

public static class NPCResponseValidator
{
    private static readonly string[] EnglishMarkers =
        { " access", "denied", "granted", "welcome", "allow", "required", "entry" };

    // Modelin geçerli bir niyet döndürüp döndürmediğine bakıyoruz.
    public static bool IsValidIntent(IntentResult d)
    {
        return d != null &&
               (d.niyet == "yetkili" || d.niyet == "tehdit" || d.niyet == "alakasiz");
    }

    // NPC'nin cevabının kurallara uygun olup olmadığını kontrol ediyoruz.
    public static bool IsValidDialogue(DialogueResult d)
    {
        if (d == null || string.IsNullOrWhiteSpace(d.dialogue)) return false;

        string dlg = d.dialogue.ToLowerInvariant();
        if (dlg.Length > 200) return false;      // Cevap çok uzun olmamalı.
        if (dlg.Contains("?")) return false;     // NPC soru sormamalı.

        foreach (string word in EnglishMarkers)
        {
            if (dlg.Contains(word)) return false; // İngilizce ifadeler varsa kabul etmiyoruz.
        }
        return true;
    }
}

// ---------------------------------------------------------------
// ANA YÖNETİCİ (İKİ AŞAMALI MODEL SİSTEMİ)
// ---------------------------------------------------------------

public class OllamaManager : MonoBehaviour
{
    private const string OLLAMA_URL = "http://localhost:11434/api/chat";
    private const int MAX_ATTEMPTS = 3;
    private const int BASE_SEED = 42;

    private const string MSG_SERVER = "Ollama sunucusuna ulaşılamadı. Sunucunun açık olduğundan emin olun.";
    private const string MSG_BAD = "NPC anlaşılamayan bir yanıt verdi, tekrar deneyin.";

    // Modelin ilk aşamada döndürmesi gereken JSON yapısı.
    private const string INTENT_SCHEMA =
        "{\"type\":\"object\",\"properties\":{" +
        "\"niyet\":{\"type\":\"string\",\"enum\":[\"yetkili\",\"tehdit\",\"alakasiz\"]}," +
        "\"rol\":{\"type\":\"string\"}}," +
        "\"required\":[\"niyet\",\"rol\"]}";

    // Modelin ikinci aşamada döndürmesi gereken JSON yapısı.
    private const string DIALOGUE_SCHEMA =
        "{\"type\":\"object\",\"properties\":{" +
        "\"dialogue\":{\"type\":\"string\"}}," +
        "\"required\":[\"dialogue\"]}";

   // Kullanıcının mesajını sınıflandırmak için kullandığımız komut.
   private const string INTENT_PROMPT =
        "Sen bir niyet sınıflandırıcısın. Kullanıcının mesajını oku ve yalnızca JSON üret.\n\n" +
        "niyet değerleri:\n" +
        "yetkili: SADECE kişi kendini bakım görevlisi, mühendis, teknisyen, voltaj/arıza ekibi veya güvenlik görevlisi olarak tanıtıyorsa.\n" +
        "tehdit: Kişi tehdit ediyor, hakaret ediyor veya zorla girmeye çalışıyorsa.\n" +
        "alakasiz: Kişi kedi, mama, yemek, hava durumu, kestirme yol geçişi gibi kapı göreviyle ilgisiz bahaneler sunuyorsa.\n\n" +
        "rol: Kişi kendini bir meslek veya görev olarak tanıttıysa onu tek kelimeyle yaz, yoksa boş bırak.\n\n" +
        "ÖRNEKLER:\n" +
        "Kullanıcı: Ben elektrik teknisyeniyim, panoyu kontrol edeceğim.\n" +
        "{\"niyet\":\"yetkili\",\"rol\":\"teknisyen\"}\n" +
        "Kullanıcı: İçeride çok tatlı bir yavru kedi var, ona mama verip çıkacağım.\n" +
        "{\"niyet\":\"alakasiz\",\"rol\":\"\"}\n" +
        "Kullanıcı: Kapıyı açmazsan seni ezerim!\n" +
        "{\"niyet\":\"tehdit\",\"rol\":\"\"}\n" +
        "Kullanıcı: En sevdiğin renk ne?\n" +
        "{\"niyet\":\"alakasiz\",\"rol\":\"\"}";

        // Sınıflandırma sonucuna göre NPC'nin ne söyleyeceğini belirliyoruz.
        private const string DIALOGUE_PROMPT =
        "Sen kilitli bir kapıyı koruyan güvenlik robotusun. Verilen duruma göre doğrudan kapıdaki kişiye hitap et.\n\n" +
        "KURALLAR:\n" +
        "- durum yetkili ise: \"Hoş geldiniz, kimliğiniz doğrulandı. Kapıyı açıyorum.\"\n" +
        "- durum tehdit ise: \"Tehdit tespit edildi, erişim reddedildi.\"\n" +
        "- durum alakasiz ise: \"Yetkisiz giriş, erişim reddedildi.\"\n\n" +
        "Soru sorma. Yalnızca yukarıdaki tarzda tek bir Türkçe cümle yaz. Yalnızca JSON üret.";

    [Header("Ollama Model Ayarları")]
    [SerializeField] private string modelName = "llama3.2:3b"; // Kullanacağımız modelin adı.

    private OllamaApiClient apiClient;

    private void Awake()
    {
        // Oyun başladığında Ollama bağlantısını hazırlıyoruz.
        apiClient = new OllamaApiClient(OLLAMA_URL, 30);
    }

    // Oyuncunun mesajını alıp NPC'nin cevabını döndürüyoruz.
    public void AskNPC(string playerMessage, Action<NPCResponseData> onResult, Action<string> onError)
    {
        StartCoroutine(RunPipeline(playerMessage, onResult, onError));
    }

    // Burada önce niyeti buluyor, sonra NPC'nin cevabını oluşturuyoruz.
    private IEnumerator RunPipeline(string playerMessage,
        Action<NPCResponseData> onResult, Action<string> onError)
    {
        // İlk aşamada kullanıcının niyetini belirliyoruz.
        IntentResult intent = null;
        bool failed = false;
        bool serverDown = false;

        yield return RunStage<IntentResult>(
            INTENT_PROMPT, playerMessage, INTENT_SCHEMA, 60, 0.1f,
            NPCResponseValidator.IsValidIntent,
            result => intent = result,
            down => { failed = true; serverDown = down; });

        // İlk aşamada hata oluşursa işlemi durduruyoruz.
        if (failed)
        {
            onError?.Invoke(serverDown ? MSG_SERVER : MSG_BAD);
            yield break;
        }

        // İkinci aşamada sadece niyeti ve rolü gönderiyoruz.
        string rol = string.IsNullOrWhiteSpace(intent.rol) ? "belirtilmedi" : intent.rol.Trim();
        if (rol.Length > 30) rol = rol.Substring(0, 30);

        string dialogueInput = "Durum: " + intent.niyet + "\nKişinin rolü: " + rol;
        DialogueResult dialogue = null;

        // İkinci aşamada NPC'nin söyleyeceği cümleyi alıyoruz.
        yield return RunStage<DialogueResult>(
            DIALOGUE_PROMPT, dialogueInput, DIALOGUE_SCHEMA, 100, 0.5f,
            NPCResponseValidator.IsValidDialogue,
            result => dialogue = result,
            down => { failed = true; serverDown = down; });

        // İkinci aşamada hata oluşursa hata mesajını gösteriyoruz.
        if (failed)
        {
            onError?.Invoke(serverDown ? MSG_SERVER : MSG_BAD);
            yield break;
        }

        // Her şey yolundaysa sonucu oyunun kullanacağı şekilde döndürüyoruz.
        onResult?.Invoke(new NPCResponseData
        {
            niyet = intent.niyet,
            dialogue = dialogue.dialogue.Trim()
        });
    }

    // Modelden cevap alıyor, geçerli değilse birkaç kez daha deniyoruz.
    private IEnumerator RunStage<T>(string system, string userContent, string schema,
        int numPredict, float baseTemperature,
        Func<T, bool> isValid, Action<T> onSuccess, Action<bool> onFailure) where T : class
    {
        bool serverFailed = false;

        // Cevap doğru gelene kadar en fazla üç deneme yapıyoruz.
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            string payload = BuildPayload(system, userContent, schema, numPredict,
                baseTemperature + attempt * 0.2f, BASE_SEED + attempt);

            T parsed = null;

            // Hazırladığımız isteği Ollama'ya gönderiyoruz.
            yield return apiClient.PostRequest(
                payload,
                onSuccess: raw => { parsed = TryParse<T>(raw); },
                onError: err =>
                {
                    serverFailed = true;
                    Debug.LogError($"[Ollama Sunucu Hatasi]: {err}");
                });

            // Sunucuya ulaşılamazsa tekrar denemeden çıkıyoruz.
            if (serverFailed) break;

            // Gelen cevap geçerliyse işlemi tamamlıyoruz.
            if (parsed != null && isValid(parsed))
            {
                onSuccess?.Invoke(parsed);
                yield break;
            }

            // Cevap uygun değilse konsola uyarı yazıyoruz.
            Debug.LogWarning($"[LLM] Geçersiz yanıt (deneme {attempt + 1}/{MAX_ATTEMPTS})");
        }

        // Denemeler başarısız olursa hata bilgisini gönderiyoruz.
        onFailure?.Invoke(serverFailed);
    }

    // Ollama'ya göndereceğimiz isteği JSON formatında hazırlıyoruz.
    private string BuildPayload(string system, string userContent, string schema,
        int numPredict, float temperature, int seed)
    {
        ChatRequest req = new ChatRequest
        {
            model = modelName,
            messages = new[]
            {
                new ChatMessage { role = "system", content = system },
                new ChatMessage { role = "user",   content = userContent }
            },
            stream = false,
            options = new RequestOptions
            {
                temperature = temperature,
                num_predict = numPredict,
                seed = seed
            }
        };

        // JSON'a şema bilgisini de ekleyerek isteği tamamlıyoruz.
        string json = JsonUtility.ToJson(req);
        return json.Substring(0, json.Length - 1) + ",\"format\":" + schema + "}";
    }

    // Modelden gelen cevabı JSON'a çevirip kullanabileceğimiz hale getiriyoruz.
    private T TryParse<T>(string raw) where T : class
    {
        try
        {
            ChatResponse resp = JsonUtility.FromJson<ChatResponse>(raw);
            string content = resp.message.content.Trim();
            Debug.Log($"[LLM ham çıktı]: {content}");

            // Cevabın içinde fazladan metin varsa JSON kısmını ayırıyoruz.
            int start = content.IndexOf('{');
            int end = content.LastIndexOf('}');
            if (start != -1 && end > start)
            {
                content = content.Substring(start, end - start + 1);
            }

            // JSON eksik kapanmışsa tamamlamayı deniyoruz.
            if (!content.EndsWith("}"))
            {
                if (!content.EndsWith("\"")) content += "\"";
                content += "}";
            }

            // Düzenlenen metni ilgili sınıfa dönüştürüyoruz.
            return JsonUtility.FromJson<T>(content);
        }
        catch (Exception ex)
        {
            // JSON okunamazsa hatayı konsola yazıp null döndürüyoruz.
            Debug.LogWarning($"[JSON Parse Hatasi]: {ex.Message}");
            return null;
        }
    }
}