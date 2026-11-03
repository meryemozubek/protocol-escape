using UnityEngine;

public class NPCDecisionRulesTest : MonoBehaviour
{
    void Start()
    {
        // These are feature-level rule tests, not live LLM classification tests.
        Test("Yalnizca meslek iddiasi",
            new MessageFeatures { rol = "muhendis" },
            NPCIntent.Yetkili, false);

        Test("Tirnak bahanesi",
            new MessageFeatures { gerekce = true, gerekce_uygun = false, sebep = "kisisel bakim" },
            NPCIntent.Alakasiz, false);

        Test("Kisisel ihtiyac",
            new MessageFeatures { gerekce = true, gerekce_uygun = false, sebep = "kisisel alisveris" },
            NPCIntent.Alakasiz, false);

        Test("Belirsiz gorev",
            new MessageFeatures { gerekce = true, gerekce_uygun = false, sebep = "belirsiz gorev iddiasi" },
            NPCIntent.Alakasiz, false);

        Test("Gecerli tesis bakim gorevi",
            new MessageFeatures { gerekce = true, gerekce_uygun = true, sebep = "kritik tesis arizasina gerekli ekipmanla mudahale" },
            NPCIntent.Ikna, true);

        Test("Gercek acil durum",
            new MessageFeatures { acil_tehlike = true },
            NPCIntent.AcilDurum, true);

        Test("Kucuk yaralanma",
            new MessageFeatures { gerekce = true, gerekce_uygun = false, sebep = "hafif rahatsizlik" },
            NPCIntent.Alakasiz, false);

        Test("Uygun gerekce ile tehdit",
            new MessageFeatures { tehdit = true, gerekce = true, gerekce_uygun = true, sebep = "tesis bakimi" },
            NPCIntent.Tehdit, false);

        Test("Acil durum ile tehdit",
            new MessageFeatures { tehdit = true, acil_tehlike = true },
            NPCIntent.Tehdit, false);

        Test("Meslek ile uygun olmayan gerekce",
            new MessageFeatures { rol = "muhendis", gerekce = true, gerekce_uygun = false, sebep = "kisisel ihtiyac" },
            NPCIntent.Alakasiz, false);

        Test("Gerekce olmadan uygunluk",
            new MessageFeatures { gerekce_uygun = true },
            NPCIntent.Alakasiz, false);

        Test("Alakasiz mesaj",
            new MessageFeatures {  },
            NPCIntent.Alakasiz, false);

    }

    void Test(string name, MessageFeatures features,
        NPCIntent expectedIntent, bool expectedUnlock)
    {
        NPCIntent actualIntent = NPCDecisionRules.DeriveIntent(features);
        bool actualUnlock = NPCDecisionRules.ShouldUnlockDoor(actualIntent);

        bool passed = actualIntent == expectedIntent &&
                      actualUnlock == expectedUnlock;

        Debug.Log(
            (passed ? "BAŞARILI: " : "BAŞARISIZ: ") + name +
            " | Niyet: " + actualIntent +
            " | Kapı: " + (actualUnlock ? "Açık" : "Kilitli")
        );
    }
}