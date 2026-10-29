using System;

// ---------------------------------------------------------------
// Veri modelleri (yalnızca veri taşır, mantık içermez)
// ---------------------------------------------------------------

// Aşama 1: modelin oyuncu mesajından çıkardığı özellikler.
// Niyet ve kapı kararı modele bırakılmaz; bu özelliklerden C# kuralları türetir.
[Serializable]
public class MessageFeatures
{
    public bool tehdit;        // hakaret, şiddet tehdidi veya zorla girme niyeti
    public bool acil_tehlike;  // yangın, yaralanma gibi yaşam güvenliği tehlikesi
    public bool gerekce;       // kapının açılması için somut ve mantıklı bir sebep sunuyor mu
    public string rol;  
    public string sebep;       // kişinin İDDİA ettiği meslek/görev (doğrulanmış değildir)
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
    public string niyet;         // yetkili / tehdit / alakasiz / ikna / acil_durum
    public string dialogue;      // NPC'nin modelce üretilmiş cümlesi
    public bool door_unlocked;   // C# kuralıyla belirlenir, modelin serbest metnine bağlı değildir
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