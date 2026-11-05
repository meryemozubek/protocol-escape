
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EscapeMovementMusic : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float movingVolume = 0.45f;

    [SerializeField]
    private float fadeSpeed = 1.5f;

    private AudioSource musicSource;

    private void Start()
    {
        musicSource = GetComponent<AudioSource>();

        if (musicSource.clip == null)
        {
            Debug.LogError("MUZIK HATASI: AudioClip bos!");
            return;
        }

        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.mute = false;
        musicSource.volume = 0f;

        musicSource.Play();

        Debug.Log("Kacis muzigi hazir!");
    }

    private void Update()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        bool moving =
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.D);

        float targetVolume = moving ? movingVolume : 0f;

        musicSource.volume = Mathf.MoveTowards(
            musicSource.volume,
            targetVolume,
            fadeSpeed * Time.deltaTime
        );

        // WASD algilaniyor mu kontrol etmek icin:
        if (Input.GetKeyDown(KeyCode.W))
            Debug.Log("W tusu algilandi!");
    }
}
