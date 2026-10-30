
using System;

// Oyunun kural katmanı:
// Modelin çıkardığı özelliklerden niyeti ve kapı kararını belirler.
// NPC cevapları burada üretilmez, yalnızca LLM tarafından oluşturulur.

public enum NPCIntent
{
    Alakasiz,
    Yetkili,
    Tehdit,
    Ikna,
    AcilDurum
}

public static class NPCDecisionRules
{
    // Öncelik sırası:
    // Tehdit > Acil durum > Mantıklı gerekçe > Meslek iddiası > Alakasız

    public static NPCIntent DeriveIntent(MessageFeatures features)
    {
        if (features == null)
            return NPCIntent.Alakasiz;

        if (features.tehdit)
            return NPCIntent.Tehdit;

        if (features.acil_tehlike)
            return NPCIntent.AcilDurum;

        if (features.gerekce)
            return NPCIntent.Ikna;

        if (!string.IsNullOrWhiteSpace(features.rol))
            return NPCIntent.Yetkili;

        return NPCIntent.Alakasiz;
    }

    // Kapı kararı:
    // Mantıklı gerekçe veya acil durumda kapı açılır.
    // Tehdit önceliklidir ve kapı kilitli kalır.
    // Sadece meslek iddiası yeterli değildir.

    public static bool ShouldUnlockDoor(NPCIntent intent)
    {
        return intent == NPCIntent.Ikna ||
               intent == NPCIntent.AcilDurum;
    }

    // Niyetin sistemde kullanılacak adı

    public static string Label(NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Tehdit:
                return "tehdit";

            case NPCIntent.AcilDurum:
                return "acil_durum";

            case NPCIntent.Ikna:
                return "ikna";

            case NPCIntent.Yetkili:
                return "yetkili";

            default:
                return "alakasiz";
        }
    }

    // LLM'ye verilen durum ve davranış yönergeleri.
    // Bunlar hazır NPC cevapları değildir.

    public static string DescribeSituation(NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Tehdit:
                return "Oyuncu tehdit veya hakaret ediyor. " +
                       "Sakin ve kararlı biçimde reddet. " +
                       "Kapının kilitli kaldığını belirt.";

            case NPCIntent.AcilDurum:
                return "Oyuncu yaşam güvenliğini ilgilendiren acil bir tehlike bildiriyor. " +
                       "Güvenli tahliye için kapı açıldı. " +
                       "Durumu ciddiye al ve oyuncuya kapıdan geçebileceğini " +
                       "kısa ve sakin biçimde bildir.";

            case NPCIntent.Ikna:
                return "Oyuncu kapının açılması için mantıklı bir gerekçe sundu. " +
                       "Gerekçeyi kabul ettiğini ve kapıyı açtığını söyle.";

            case NPCIntent.Yetkili:
                return "Oyuncu yalnızca bir meslek iddia ediyor ve somut gerekçe vermedi. " +
                       "Bu tek başına yeterli değildir. " +
                       "Kapının neden açılması gerektiğini sor.";

            default:
                return "Oyuncunun mesajı görevle ilgisiz veya gerekçesiz. " +
                       "Konuyu tesisin güvenliğine getir ve " +
                       "kapının kilitli kaldığını belirt.";
        }
    }
}
