
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private DialogueUILayout dialogueUI;

    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideOnExit = false;

    private void Reset()
    {
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        Debug.Log("OYUNCU NPC ALANINA GİRDİ!");

        if (dialogueUI == null)
        {
            Debug.LogWarning(
                "DialogueTrigger: DialogueUILayout atanmamış!"
            );
            return;
        }

        dialogueUI.ShowDialoguePanel();

        Debug.Log("DialoguePanel açma komutu gönderildi!");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hideOnExit || !other.CompareTag(playerTag))
            return;

        if (dialogueUI != null)
            dialogueUI.HideDialoguePanel();
    }
}
