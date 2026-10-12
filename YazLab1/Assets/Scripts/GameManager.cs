using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
  
    public static GameManager Instance { get; private set; }

    [Header("Oyun Durumu")]
    public bool isGameActive = true;

    private void Awake()
    {
        // Sahnede tek bir GameManager olmasini saglar
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void WinLevel()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Level Completed!");
    }

    public void LoseLevel()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Level Failed!");
        ResetLevel();
    }

    public void ResetLevel()
    {
        isGameActive = true;
        // Mevcut sahneyi bastan yukler
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}