using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bolumun aktif, kazanilmis veya kaybedilmis durumunu yonet.
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

    // Kazanma bilgisini bir kez isle; sonraki bitis cagrilarini yok say.
    public void WinLevel()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Level Completed!");
    }

    // Kaybetme durumunu bildir ve mevcut sahneyi yeniden baslat.
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