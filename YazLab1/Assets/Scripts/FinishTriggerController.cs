
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FinishTriggerController : MonoBehaviour
{
    public GameObject victoryPanel;

    [SerializeField] private BoxCollider2D exitCollider;
    [SerializeField] private SpriteRenderer closedExitVisual;

    private bool isUnlocked;
    private bool victoryShown;

    public bool IsUnlocked => isUnlocked && exitCollider != null &&
        exitCollider.enabled && exitCollider.isTrigger;

    private void Awake()
    {
        if (exitCollider == null)
            exitCollider = GetComponent<BoxCollider2D>();
        if (closedExitVisual == null)
            closedExitVisual = GetComponent<SpriteRenderer>();

        isUnlocked = false;
        victoryShown = false;
        if (exitCollider != null)
        {
            exitCollider.enabled = true;
            exitCollider.isTrigger = false;
        }
        if (closedExitVisual != null)
            closedExitVisual.enabled = true;
    }

    public bool UnlockExit()
    {
        if (exitCollider == null)
        {
            Debug.LogError("[ARGUS] Çıkış collider bağlantısı eksik.");
            return false;
        }

        exitCollider.enabled = true;
        exitCollider.isTrigger = true;
        isUnlocked = true;
        if (closedExitVisual != null)
            closedExitVisual.enabled = false;

        return IsUnlocked;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsUnlocked || victoryShown || !other.CompareTag("Player"))
            return;

        if (victoryPanel == null)
        {
            Debug.LogError("[ARGUS] Zafer paneli bağlantısı eksik.");
            return;
        }

        victoryShown = true;
        victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}
