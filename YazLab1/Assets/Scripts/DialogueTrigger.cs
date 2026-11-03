using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private DialogueUILayout dialogueUI;

    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideOnExit = false;

    
    // Bilesen eklenirken collideri gecis algilayan trigger olarak ayarla.
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Sadece oyuncu girdiginde konusma panelini ac.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (dialogueUI == null)
        {
            Debug.LogWarning("DialogueTrigger: DialogueUILayout atanmamış!");
            return;
        }

        dialogueUI.ShowDialoguePanel();
    }

    // Cikista gizleme seciliyse paneli kapat; degilse konusma acik kalsin.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hideOnExit || !other.CompareTag(playerTag)) return;
        if (dialogueUI != null) dialogueUI.HideDialoguePanel();
    }
}