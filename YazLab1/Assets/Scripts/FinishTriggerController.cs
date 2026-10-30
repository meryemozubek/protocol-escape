
using UnityEngine;

public class FinishTriggerController : MonoBehaviour
{
    public GameObject victoryPanel;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            victoryPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
