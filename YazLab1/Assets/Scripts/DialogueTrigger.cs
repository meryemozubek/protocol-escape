
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private GameObject argusPanel;

    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideOnExit = false;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        Debug.Log("OYUNCU NPC ALANINA GİRDİ!");

        if (argusPanel == null)
        {
            Debug.LogWarning("ARGUS paneli atanmamış!");
            return;
        }

        argusPanel.SetActive(true);

        Debug.Log("ARGUS paneli açıldı!");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hideOnExit || !other.CompareTag(playerTag))
            return;

        if (argusPanel != null)
            argusPanel.SetActive(false);
    }
}
