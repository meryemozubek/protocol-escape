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
        GetComponent<Collider2D>().isTrigger = true;
    }

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

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!hideOnExit || !other.CompareTag(playerTag)) return;
        if (dialogueUI != null) dialogueUI.HideDialoguePanel();
    }
}