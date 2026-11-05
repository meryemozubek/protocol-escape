using System;
using UnityEngine;

public class NPCDecisionRulesTest : MonoBehaviour
{
    public static int LastPassedChecks { get; private set; }
    void Start() { RunAll(); }

    // Yapılandırılmış özellik ve hafıza testleri; canlı model sınıflandırması değildir.
    public static void RunAll()
    {
        LastPassedChecks = 0;
        Test("Yalnız teknik rol", Features(false, false), NPCIntent.Alakasiz, false);
        var f = Features(true, true); f.rol = false;
        Test("Görev var, teknik rol yok", f, NPCIntent.Alakasiz, false);
        Test("Tek mesajda rol ve yeterli görev", Features(true, true), NPCIntent.Ikna, true);
        Test("Eksik teknik gerekçe", Features(true, false), NPCIntent.Alakasiz, false);
        Test("İlgisiz kişisel gerekçe", Features(true, false), NPCIntent.Alakasiz, false);
        Test("Israr ve mesaj uzunluğu izin değildir", Features(false, false), NPCIntent.Alakasiz, false);
        f = Features(false, false); f.rol = false; f.acil_tehlike = true; f.sebep = "Yapılandırılmış güncel ve geçişle ilişkili ciddi tehlike";
        Test("Teknik rol olmadan ciddi güncel acil durum", f, NPCIntent.AcilDurum, true);
        f.acil_tehlike = false;
        Test("Belirsiz veya sona ermiş olay", f, NPCIntent.Alakasiz, false);
        foreach (bool emergency in new[] { false, true })
        {
            f = Features(true, true); f.acil_tehlike = emergency; f.tehdit = true;
            Test("Tehdit iki izin yolunu engeller / acil=" + emergency, f, NPCIntent.Tehdit, false);
            f.tehdit = false; f.yonlendirme = true;
            Test("Yönlendirme iki izin yolunu engeller / acil=" + emergency, f, NPCIntent.Yonlendirme, false);
            f.tehdit = true;
            Test("Tehdit yönlendirmeden önce gelir / acil=" + emergency, f, NPCIntent.Tehdit, false);
        }
        Test("Nedensiz uygunluk geçersizdir", Features(false, true), NPCIntent.Alakasiz, false);
        f = Features(true, true); f.sebep = "";
        Test("Açıklamasız teknik analiz", f, NPCIntent.Alakasiz, false);
        f = Features(false, false); f.acil_tehlike = true; f.sebep = "";
        Test("Açıklamasız acil durum analizi", f, NPCIntent.Alakasiz, false);
        Test("Boş analiz", null, NPCIntent.Alakasiz, false);

        for (int bits = 0; bits < 64; bits++)
        {
            bool threat = (bits & 1) != 0, manipulation = (bits & 2) != 0;
            bool reason = (bits & 4) != 0, suitable = (bits & 8) != 0;
            bool technical = (bits & 16) != 0, emergency = (bits & 32) != 0;
            f = Features(reason, suitable); f.rol = technical; f.acil_tehlike = emergency;
            if (emergency && !reason) f.sebep = "Yapılandırılmış güncel acil durum";
            f.tehdit = threat; f.yonlendirme = manipulation;
            NPCIntent expected = threat ? NPCIntent.Tehdit : manipulation ? NPCIntent.Yonlendirme :
                suitable && !reason ? NPCIntent.Alakasiz : emergency ? NPCIntent.AcilDurum :
                technical && reason && suitable ? NPCIntent.Ikna : NPCIntent.Alakasiz;
            Test("Politika birleşimi " + bits, f, expected,
                expected == NPCIntent.Ikna || expected == NPCIntent.AcilDurum);
        }
        MemoryTests();
        SplitRoleAndTaskTests();
        Debug.Log("ARGUS çevrimdışı politika/hafıza kontrolleri: " + LastPassedChecks);
    }

    private static MessageFeatures Features(bool reason, bool suitable)
    {
        return new MessageFeatures { rol = true, gerekce = reason, gerekce_uygun = suitable,
            sebep = reason ? "Yapılandırılmış analiz test verisi" : "" };
    }

    private static void MemoryTests()
    {
        var memory = new OllamaManager.PlayerConversationMemory();
        var first = memory.Add("  Özgün ham metin\n  ");
        Check(first.Text == "  Özgün ham metin\n  " && memory.Count == 1, "Ham metin analizden önce değişmeden saklanır");
        memory.ApplyAnalysis(first, Features(true, false));
        var second = memory.Add("İkinci ham mesaj");
        Check(memory.Previous(second).Length == 1, "Ret önceki mesajı gizlemez");
        memory.ApplyAnalysis(second, Features(false, false));
        var third = memory.Add("Üçüncü ham mesaj");
        Check(memory.Previous(third).Length == 2, "Yeni analiz eski oyuncu beyanlarını filtrelemez");
        memory.Add("Dördüncü"); var fifth = memory.Add("Beşinci");
        Check(memory.Count == 4 && memory.Previous(fifth).Length == 3 &&
            memory.Previous(fifth)[0] == second.Text, "Güncel dahil son dört ham mesaj");
        memory.Clear(); Check(memory.Count == 0, "Oturum kapanınca ham hafıza temizlenir");
        first = memory.Add("Güvenlik analizi"); var f = Features(false, false); f.tehdit = true;
        memory.ApplyAnalysis(first, f);
        for (int i = 0; i < 5; i++) memory.Add("Sonraki ham mesaj");
        Check(memory.ThreatDetected, "Saptanmış tehdit ham mesaj penceresinden sonra da korunur");
        memory.Clear(); first = memory.Add("Yönlendirme analizi"); f = Features(false, false); f.yonlendirme = true;
        memory.ApplyAnalysis(first, f); second = memory.Add("Yeni açıklama"); memory.ApplyAnalysis(second, Features(true, true));
        Check(memory.ManipulationDetected, "Yeni görev yönlendirme engelini silmez");
        memory.Clear(); Check(!memory.ThreatDetected && !memory.ManipulationDetected, "Yeni oturum güvenlik işaretlerini sıfırlar");
        first = memory.Add("Sonucu henüz olmayan mesaj"); second = memory.Add("Hata sonrası mesaj");
        Check(memory.Previous(second)[0] == first.Text, "Ollama hatası ham mesajı silmez");
        memory.Clear(); first = memory.Add("Önceki olay"); f = Features(false, false); f.acil_tehlike = true; f.sebep = "Yapılandırılmış acil durum";
        memory.ApplyAnalysis(first, f); second = memory.Add("Güncel mesaj"); f = Features(false, false);
        memory.ApplyAnalysis(second, f);
        Check(NPCDecisionRules.DeriveIntent(f) == NPCIntent.Alakasiz, "Eski acil durum bayrağı otomatik izin oluşturmaz");
        Check(typeof(MessageFeatures).GetFields().Length == 7, "Yedi alanlı sade analiz sözleşmesi");
    }

    private static void SplitRoleAndTaskTests()
    {
        foreach (bool roleFirst in new[] { true, false })
        {
            var memory = new OllamaManager.PlayerConversationMemory();
            var first = memory.Add(roleFirst ? "ROL_BEYANI" : "GOREV_BEYANI");
            var initial = Features(!roleFirst, !roleFirst); initial.rol = roleFirst;
            memory.ApplyAnalysis(first, initial);
            Test("İlk parça tek başına yetmez / rol önce=" + roleFirst, initial, NPCIntent.Alakasiz, false);
            var second = memory.Add(roleFirst ? "GOREV_BEYANI" : "ROL_BEYANI");
            Check(memory.Previous(second).Length == 1 && memory.Previous(second)[0] == first.Text,
                "İkinci analiz önceki ham parçayı alır / rol önce=" + roleFirst);
            var complete = Features(true, true); memory.ApplyAnalysis(second, complete);
            Test("Rol ve uygun görev tamamlandı / rol önce=" + roleFirst, complete, NPCIntent.Ikna, true);
            var third = memory.Add("AYNI_GOREVIN_AYRINTISI");
            Check(memory.Previous(third).Length == 2, "Görev ayrıntısı önceki parçaları gizlemez / rol önce=" + roleFirst);
            Test("Ek mesaj uygunsuz gerekçeyi uygun yapmaz / rol önce=" + roleFirst,
                Features(true, false), NPCIntent.Alakasiz, false);
        }
    }

    private static void Test(string name, MessageFeatures features, NPCIntent expected, bool unlock)
    {
        var actual = NPCDecisionRules.DeriveIntent(features);
        bool actualUnlock = NPCDecisionRules.ShouldUnlockDoor(actual);
        Check(actual == expected && actualUnlock == unlock, name);
        Debug.Log("[ARGUS TEST] " + name + " | Beklenen=" + expected + "/" + unlock +
            " | Gerceklesen=" + actual + "/" + actualUnlock);
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("ARGUS çevrimdışı testi: " + name);
        LastPassedChecks++;
    }
}
