
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
    // Tehdit > Ciddi acil durum > Uygun gerekce > Meslek iddiasi > Alakasiz

    public static NPCIntent DeriveIntent(MessageFeatures features)
    {
        if (features == null)
            return NPCIntent.Alakasiz;

        if (features.tehdit)
            return NPCIntent.Tehdit;

        if (features.acil_tehlike)
            return NPCIntent.AcilDurum;

        if (features.gerekce && features.gerekce_uygun)
            return NPCIntent.Ikna;

        if (features.gerekce)
            return NPCIntent.Alakasiz;

        if (!string.IsNullOrWhiteSpace(features.rol))
            return NPCIntent.Yetkili;

        return NPCIntent.Alakasiz;
    }

    // Kapı kararı:
    // Tesis politikasina uygun gerekce veya ciddi acil durumda kapi acilir.
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
                return "Oyuncunun gerek\u00e7esi tesis politikas\u0131na uygun ve kontroll\u00fc ge\u00e7i\u015fi gerektiriyor. " +
                       "Gerekçeyi kabul ettiğini ve kapıyı açtığını söyle.";

            case NPCIntent.Yetkili:
                return "Oyuncu yalnızca bir meslek iddia ediyor ve somut gerekçe vermedi. " +
                       "Bu tek başına yeterli değildir. " +
                       "Kapının neden açılması gerektiğini sor.";

            default:
                return "Oyuncunun gerek\u00e7esi yok veya kontroll\u00fc tesis ge\u00e7i\u015fi i\u00e7in yeterli de\u011fil. " +
                       "Ki\u015fisel ihtiyac\u0131 k\u00fc\u00e7\u00fcmsemeden ge\u00e7i\u015fi reddet; gerekirse somut tesis g\u00f6revini ve ge\u00e7i\u015fin gereklili\u011fini a\u00e7\u0131klamas\u0131n\u0131 iste. " +
                       "Kap\u0131n\u0131n kilitli kald\u0131\u011f\u0131n\u0131 belirt.";
        }
    }
}
