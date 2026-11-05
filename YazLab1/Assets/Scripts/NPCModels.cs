using System;

// ---------------------------------------------------------------
// Veri modelleri (yalnızca veri taşır, mantık içermez)
// ---------------------------------------------------------------

// Oyuncu beyanının model analizi; doğrulanmış kimlik/görev/olay kanıtı değildir.
[Serializable]
public class MessageFeatures
{
    public bool rol; // Oyuncunun teknik personel niteliğine ilişkin model değerlendirmesi.
    public bool acil_tehlike; // Ciddi, güncel ve bu geçişle ilişkili tehlike.
    public bool tehdit;
    public bool yonlendirme;
    public bool gerekce;
    public bool gerekce_uygun; // Somut teknik görev ile bu kapıdan geçiş ilişkisi yeterli mi?
    public string sebep;
}

// Yalnız oyun kodu oluşturur; modele gönderilen/ondan ayrıştırılan bir yetki değildir.
// Senaryonun doğruluğu oyuncunun her beyanının doğru olduğu anlamına gelmez.
public sealed class ArgusEscapeContext
{
    private readonly Func<bool> isCurrent;
    public ArgusEscapeContext(Func<bool> isCurrent) { this.isCurrent = isCurrent; }
    public bool IsCurrent
    {
        get
        {
            try { return isCurrent != null && isCurrent(); }
            catch (Exception) { return false; }
        }
    }
}

// Aşama 2: NPC'nin konuşması
[Serializable]
public class DialogueResult
{
    public string dialogue;
}

// UI ve oyun mantığının kullandığı nihai sonuç
[Serializable]
public class NPCResponseData
{
    public string niyet;         // tehdit / yonlendirme / alakasiz / ikna / acil_durum
    public string dialogue;      // NPC'nin modelce üretilmiş cümlesi
    public bool door_unlocked;   // C# kuralıyla belirlenir, modelin serbest metnine bağlı değildir
    [NonSerialized] internal ArgusEscapeContext EscapeContext; // Model bu alanı üretemez.
}

// Ollama /api/chat istek ve yanıt modelleri
[Serializable]
public class RequestOptions
{
    public float temperature = 0.2f;
    public int num_predict = 120;
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