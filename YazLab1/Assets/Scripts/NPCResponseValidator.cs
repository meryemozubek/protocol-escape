
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// LLM çıktısının geçerliliğini denetler.
// NPC cevabı üretmez; hatalı çıktıları reddeder.
public static class NPCResponseValidator
{
    private const int MaxRoleLength = 60;
    private const int MaxDialogueLength = 200;
    private const int MaxSentences = 3;

    private static readonly HashSet<string> EnglishWords =
        new HashSet<string>
    {
        "the", "you", "your", "access", "denied", "granted",
        "door", "please", "welcome", "and", "not", "will",
        "this", "that", "verified", "locked", "open",
        "unlock", "unlocked", "emergency", "security",
        "sorry", "information", "reason", "now",
        "waiting", "system", "therefore", "something",
        "nobody", "only", "manager"
    };

    // Kapı kilitliyken kullanılmaması gereken
    // açılma veya geçiş izni ifadeleri.
    private static readonly string[] OpenClaims =
    {
        "açıyorum",
        "açılıyor",
        "açtım",
        "açıldı",
        "açılmıştır",
        "açılmış",
        "kapıyı açabilirim",
        "kapıyı açmaktır",
        "geçebilirsiniz",
        "geçebilirsin",
        "geçiş izni verildi",
        "erişim onaylandı"
    };

    // Kapı açıldığında kullanılmaması gereken
    // kilitli veya erişim reddi ifadeleri.
    private static readonly string[] LockClaims =
    {
        "açmayacağım",
        "açmıyorum",
        "açamam",
        "açamıyorum",
        "açılmayacak",
        "açılmıyor",
        "kilitli",
        "reddedildi",
        "izin veremem",
        "izin vermiyorum"
    };

    public static bool IsValidFeatures(MessageFeatures features)
    {
        if (features == null)
        {
            Debug.LogWarning(
                "[NPC Validator] Özellik analizi boş."
            );
            return false;
        }

        if (features.rol != null &&
            features.rol.Length > MaxRoleLength)
        {
            Debug.LogWarning(
                "[NPC Validator] Rol alanı çok uzun."
            );
            return false;
        }

        return true;
    }

    public static bool IsValidDialogue(
        DialogueResult result,
        string playerMessage,
        string lastNpcReply,
        bool doorUnlocked)
    {
        if (result == null ||
            string.IsNullOrWhiteSpace(result.dialogue))
        {
            Debug.LogWarning(
                "[NPC Validator] Diyalog boş."
            );
            return false;
        }

        string text = result.dialogue.Trim();

        // 1. Uzunluk kontrolü
        if (text.Length > MaxDialogueLength)
        {
            Debug.LogWarning(
                "[NPC Validator] Diyalog çok uzun: " +
                text.Length
            );
            return false;
        }

        // 2. Karakter kontrolü
        if (!HasOnlyAllowedCharacters(text))
        {
            Debug.LogWarning(
                "[NPC Validator] İzin verilmeyen karakter: " +
                text
            );
            return false;
        }

        // 3. Cümle sayısı kontrolü
        if (CountSentences(text) > MaxSentences)
        {
            Debug.LogWarning(
                "[NPC Validator] Cümle sayısı sınırı aşıldı: " +
                text
            );
            return false;
        }

        // 4. İngilizce kelime kontrolü
        string[] words = Tokenize(text);

        foreach (string word in words)
        {
            if (EnglishWords.Contains(word))
            {
                Debug.LogWarning(
                    "[NPC Validator] İngilizce kelime tespit edildi: " +
                    word + " | Yanıt: " + text
                );
                return false;
            }
        }

        // 5. Oyuncunun mesajını kopyalama kontrolü
        if (IsEcho(words, Tokenize(playerMessage)))
        {
            Debug.LogWarning(
                "[NPC Validator] Oyuncu mesajına aşırı benzer yanıt: " +
                text
            );
            return false;
        }

        // 6. Önceki NPC cevabını tekrar etme kontrolü
        if (!string.IsNullOrWhiteSpace(lastNpcReply) &&
            string.Join(" ", words) ==
            string.Join(" ", Tokenize(lastNpcReply)))
        {
            Debug.LogWarning(
                "[NPC Validator] Önceki NPC cevabı tekrarlandı: " +
                text
            );
            return false;
        }

        // 7. Kapı durumuyla çelişme kontrolü
        if (ContradictsDoorState(
            text.ToLowerInvariant(), doorUnlocked))
        {
            Debug.LogWarning(
                "[NPC Validator] Kapı durumuyla çelişen cevap: " +
                text
            );
            return false;
        }

        Debug.Log("[NPC Validator] Diyalog doğrulandı.");
        return true;
    }

    private static bool ContradictsDoorState(
        string lowerText,
        bool doorUnlocked)
    {
        string[] forbidden = doorUnlocked
            ? LockClaims
            : OpenClaims;

        foreach (string claim in forbidden)
        {
            if (lowerText.Contains(claim))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasOnlyAllowedCharacters(string text)
    {
        foreach (char c in text)
        {
            bool latin = c < 0x0250;

            bool punctuation =
                c >= 0x2000 && c <= 0x206F;

            if (!latin && !punctuation)
            {
                return false;
            }
        }

        return true;
    }

    private static int CountSentences(string text)
    {
        int count = 0;

        foreach (char c in text)
        {
            if (c == '.' || c == '!' || c == '?')
            {
                count++;
            }
        }

        return count;
    }

    private static string[] Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new string[0];
        }

        StringBuilder sb = new StringBuilder();

        foreach (char c in text)
        {
            sb.Append(
                char.IsLetter(c)
                    ? char.ToLowerInvariant(c)
                    : ' '
            );
        }

        return sb.ToString().Split(
            new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries
        );
    }

    // Yanıt kelimelerinin %70'i oyuncu mesajında
    // bulunuyorsa kopya olarak değerlendirir.
    private static bool IsEcho(
        string[] dialogueWords,
        string[] messageWords)
    {
        if (dialogueWords.Length < 4 ||
            messageWords.Length == 0)
        {
            return false;
        }

        HashSet<string> messageSet =
            new HashSet<string>(messageWords);

        int common = 0;

        foreach (string word in dialogueWords)
        {
            if (messageSet.Contains(word))
            {
                common++;
            }
        }

        return (float)common /
            dialogueWords.Length >= 0.7f;
    }
}
