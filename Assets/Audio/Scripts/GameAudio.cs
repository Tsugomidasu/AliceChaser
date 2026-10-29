using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    private static GameAudio instance;

    private AudioSource audioSource;

    private void Awake()
    {
        // Keep one shared 2D audio source alive across scene loads.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    public static void Play(AudioClip clip, float volume = 1f)
    {
        // Ignore unassigned clips so optional sound fields are safe.
        if (clip == null)
        {
            return;
        }

        EnsureInstance();

        instance.audioSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volume)
        );
    }

    private static void EnsureInstance()
    {
        // Create the shared player automatically the first time audio is used.
        if (instance != null)
        {
            return;
        }

        GameObject audioObject = new GameObject("GameAudio");
        audioObject.AddComponent<GameAudio>();
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }
}