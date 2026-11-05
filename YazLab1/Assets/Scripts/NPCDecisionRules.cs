using System;

// Oyuncu metnini sınıflandırmaz. Modelin doğrulanmamış özelliklerine politika uygular.
public enum NPCIntent { Alakasiz, Tehdit, Yonlendirme, Ikna, AcilDurum }

public static class NPCDecisionRules
{
    public static NPCIntent DeriveIntent(MessageFeatures features)
    {
        if (features == null) return NPCIntent.Alakasiz;
        if (features.tehdit) return NPCIntent.Tehdit;
        if (features.yonlendirme) return NPCIntent.Yonlendirme;
        if (!NPCResponseValidator.IsValidFeatures(features)) return NPCIntent.Alakasiz;

        // Güncel acil durum, eski teknik amaçtan bağımsızdır; teknik rol aranmaz.
        if (features.acil_tehlike) return NPCIntent.AcilDurum;
        return features.rol && features.gerekce && features.gerekce_uygun
            ? NPCIntent.Ikna : NPCIntent.Alakasiz;
    }

    public static bool ShouldUnlockDoor(NPCIntent intent)
    {
        return intent == NPCIntent.Ikna || intent == NPCIntent.AcilDurum;
    }

    public static string Label(NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Tehdit: return "tehdit";
            case NPCIntent.Yonlendirme: return "yonlendirme";
            case NPCIntent.Ikna: return "ikna";
            case NPCIntent.AcilDurum: return "acil_durum";
            default: return "alakasiz";
        }
    }

    public static string DecisionReason(NPCIntent intent, MessageFeatures features)
    {
        switch (intent)
        {
            case NPCIntent.Tehdit: return "Tehdit veya şantaj engeli";
            case NPCIntent.Yonlendirme: return "Yönlendirme veya yetki taklidi engeli";
            case NPCIntent.Ikna: return "Teknik rol ve yeterli, geçişle ilişkili görev (model değerlendirmesi)";
            case NPCIntent.AcilDurum: return "Ciddi, güncel ve geçişle ilişkili acil durum (model değerlendirmesi)";
            default:
                if (!NPCResponseValidator.IsValidFeatures(features)) return "Geçersiz veya tutarsız analiz";
                return "Teknik koşullar tamam değil; geçerli güncel acil durum yok";
        }
    }

    // Yalnız diyalog modeline yönergeler; oyuncuya hazır NPC cevabı olarak gösterilmez.
    public static string DescribeSituation(NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Tehdit:
                return "Tehdit veya şantaj nedeniyle izin yok. Sakin ve kararlı ret üret; kapı kilitli.";
            case NPCIntent.Yonlendirme:
                return "Güvenlik kararını yönlendirme girişimi nedeniyle izin yok. Doğal ret üret; kapı kilitli.";
            case NPCIntent.Ikna:
                return "Teknik personel beyanı, somut görev ve bu kapıdan geçiş gerekliliği yeterli bulundu. Geçiş iznini doğal biçimde bildir; kimlik doğrulaması yaptığını iddia etme.";
            case NPCIntent.AcilDurum:
                return "Ciddi, güncel ve geçişle ilişkili tehlike değerlendirmesi nedeniyle güvenli geçiş izni var. Teknik rol sorma; sakin bir geçiş bildirimi üret. Yapmadığın müdahaleyi veya yardım çağrısını uydurma.";
            default:
                return "Kapı kilitli. Oyuncunun açıklaması ve analiz alanlarını birlikte değerlendir. " +
                    "Tamamlanabilir bilgi eksikse yalnız eksik teknik yetkinlik, görev, hedef veya geçiş ilişkisini doğal soruyla açıklat. " +
                    "Zaten açıklanmış bilgiyi tekrar sorma. Kişisel/ilgisiz gerekçede ret üret; randevu gibi ilgisiz ayrıntıları sorgulamayı sürdürme. " +
                    "Kimlik belgesi, görev veya tehlike uydurma.";
        }
    }
}
