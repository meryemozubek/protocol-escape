using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Butonda bekleyen ajan icin oyuncunun gecisini sureyle taklit et.
public class TrainingPlayerPassSimulator : MonoBehaviour
{
    // Oyuncunun kapidan gecmesi icin rastgele bekleme suresi.
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;
    // Oyuncunun kapidan gecip gecmedigini belirtir.
    public bool HasPlayerPassed { get; private set; }
    // Oyuncunun gecisi gercekten bekleniyor mu?
    public bool IsWaiting => isActiveTask && passCoroutine != null && !HasPlayerPassed;
    public float MinimumDelay => minDelay;
    public float MaximumDelay => maxDelay;
    public event Action<TrainingPlayerPassSimulator> OnAgentReached;
    public event Action<TrainingPlayerPassSimulator> OnWaitCancelled;
    // Ajanin birden fazla collideri varsa her birini ayri takip et.
    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private Coroutine passCoroutine;
    private bool isActiveTask;
    private void OnTriggerEnter2D(Collider2D other) { RegisterOccupant(other); }
    private void OnTriggerStay2D(Collider2D other) { RegisterOccupant(other); }
    // Yalnizca ML-Agent butona bastiginda calisir.
    private void RegisterOccupant(Collider2D other)
    {
        if (!other.CompareTag("Agent")) return;
        bool wasEmpty = occupants.Count == 0;
        occupants.Add(other);
        if (wasEmpty && isActiveTask && !HasPlayerPassed) OnAgentReached?.Invoke(this);
        StartWaitingIfReady();
    }
    // Ajan erken ayrilirsa bekleme iptal edilir.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Agent")) return;
        occupants.Remove(other);
        if (occupants.Count == 0) CancelWaiting(true);
    }
    // Yalnizca sirasi gelen butonu aktif yap; diger butonlarda bekleme baslamasin.
    public void SetTaskActive(bool active)
    {
        if (isActiveTask == active) return;
        isActiveTask = active;
        if (!active) { CancelWaiting(false); return; }
        if (occupants.Count > 0 && !HasPlayerPassed) OnAgentReached?.Invoke(this);
        StartWaitingIfReady();
    }
    // Ayni buton icin ikinci bir bekleme coroutinei acma.
    private void StartWaitingIfReady()
    {
        if (isActiveTask && occupants.Count > 0 && !HasPlayerPassed && passCoroutine == null)
            passCoroutine = StartCoroutine(SimulatePlayerPass());
    }
    // Secilen sure boyunca butonda kaldiysa oyuncu gecmis kabul et.
    private IEnumerator SimulatePlayerPass()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
        // Bekleme tamamlaninca oyuncunun gectigi kabul edilir.
        HasPlayerPassed = true;
        passCoroutine = null;
    }
    // Beklemeyi durdur; gercek erken ayrilma ise istatistige ekle.
    private void CancelWaiting(bool countCancellation)
    {
        if (passCoroutine == null) return;
        StopCoroutine(passCoroutine);
        passCoroutine = null;
        if (countCancellation && isActiveTask && !HasPlayerPassed) OnWaitCancelled?.Invoke(this);
    }
    // Pythondan gelen sureleri kontrol ederek bekleme araligini guncelle.
    public void ConfigureDelays(float minimum, float maximum)
    {
        if (minimum < 0f || maximum < minimum) throw new ArgumentOutOfRangeException(nameof(minimum));
        minDelay = minimum;
        maxDelay = maximum;
    }
    // Onceki bolumun bekleme ve temas bilgilerini temizle.
    public void ResetForEpisode()
    {
        // Devam eden beklemeyi durdur.
        CancelWaiting(false);
        occupants.Clear();
        isActiveTask = false;
        HasPlayerPassed = false;
    }
    private void OnDisable() { ResetForEpisode(); }
}
