
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainMenuPanel;

    public void PlayGame()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }
}
