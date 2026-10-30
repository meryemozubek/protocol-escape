using UnityEngine;

public class NPCDecisionRulesTest : MonoBehaviour
{
    void Start()
    {
        Test("Yalnızca meslek iddiası",
            new MessageFeatures { rol = "mühendis" },
            NPCIntent.Yetkili, false);

        Test("Mantıklı gerekçe",
           new MessageFeatures { gerekce = true },
            NPCIntent.Ikna, true);

        Test("Tehdit",
            new MessageFeatures { tehdit = true, gerekce = true },
            NPCIntent.Tehdit, false);

        Test("Acil durum",
            new MessageFeatures { acil_tehlike = true },
            NPCIntent.AcilDurum, false);

        Test("Alakasız mesaj",
            new MessageFeatures(),
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