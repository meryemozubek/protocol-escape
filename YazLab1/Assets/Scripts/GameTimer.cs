
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    public float ElapsedTime { get; private set; }

    private void Awake()
    {
        Instance = this;
        ElapsedTime = 0f;
    }

    private void Update()
    {
        if (Time.timeScale > 0f)
        {
            ElapsedTime += Time.deltaTime;
        }
    }
}
