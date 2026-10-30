using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string AmbienceVolumeKey = "AmbienceVolume";
    private const string MusicMutedKey = "MusicMuted";
    private const string SfxMutedKey = "SfxMuted";
    private const string AmbienceMutedKey = "AmbienceMuted";
    private const string LegacyMusicEnabledKey = "MusicEnabled";
    private const string SettingsVersionKey = "AudioSettingsVersion";
    private const int CurrentSettingsVersion = 2;

    private const float DefaultMusicVolume = 1f;
    private const float DefaultSfxVolume = 1f;
    private const float DefaultAmbienceVolume = 1f;

    private static GameAudio instance;
    private static bool settingsLoaded;
    private static float musicVolume = DefaultMusicVolume;
    private static float sfxVolume = DefaultSfxVolume;
    private static float ambienceVolume = DefaultAmbienceVolume;
    private static bool musicMuted;
    private static bool sfxMuted;
    private static bool ambienceMuted;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private AudioSource ambienceSource;
    private float musicBaseVolume = 1f;
    private float ambienceBaseVolume = 1f;

    public static float MusicVolume
    {
        get
        {
            LoadSettings();
            return musicVolume;
        }
    }

    public static float SfxVolume
    {
        get
        {
            LoadSettings();
            return sfxVolume;
        }
    }

    public static float AmbienceVolume
    {
        get
        {
            LoadSettings();
            return ambienceVolume;
        }
    }

    public static bool MusicMuted
    {
        get
        {
            LoadSettings();
            return musicMuted;
        }
    }

    public static bool SfxMuted
    {
        get
        {
            LoadSettings();
            return sfxMuted;
        }
    }

    public static bool AmbienceMuted
    {
        get
        {
            LoadSettings();
            return ambienceMuted;
        }
    }

    public static bool MusicEnabled => !MusicMuted;

    private void Awake()
    {
        // Keep one audio manager alive across scene loads.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();

        sfxSource = GetComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        ambienceSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(sfxSource);
        ConfigureAudioSource(musicSource);
        ConfigureAudioSource(ambienceSource);
        ApplyCurrentSettings();
    }

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureInstance();
        instance.sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public static void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureInstance();
        LoadSettings();

        // Keep the current music loop running when the same clip is requested.
        bool sameClip = instance.musicSource.clip == clip;

        instance.musicBaseVolume = Mathf.Clamp01(volume);
        instance.musicSource.clip = clip;
        instance.musicSource.volume = instance.musicBaseVolume * musicVolume;
        instance.musicSource.pitch = 1f;
        instance.musicSource.loop = true;
        instance.musicSource.mute = musicMuted;

        if (!sameClip || !instance.musicSource.isPlaying)
        {
            instance.musicSource.Play();
        }
    }

    public static void PlayAmbience(
        AudioClip clip,
        float volume = 1f,
        float pitch = 1f)
    {
        if (clip == null)
        {
            StopAmbience();
            return;
        }

        EnsureInstance();
        LoadSettings();

        bool sameClip = instance.ambienceSource.clip == clip;

        instance.ambienceBaseVolume = Mathf.Clamp01(volume);
        instance.ambienceSource.clip = clip;
        instance.ambienceSource.volume =
            instance.ambienceBaseVolume * ambienceVolume;
        instance.ambienceSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        instance.ambienceSource.loop = true;
        instance.ambienceSource.mute = ambienceMuted;

        if (!sameClip || !instance.ambienceSource.isPlaying)
        {
            instance.ambienceSource.Play();
        }
    }

    public static void StopMusic()
    {
        if (instance == null || instance.musicSource == null)
        {
            return;
        }

        instance.musicSource.Stop();
        instance.musicSource.clip = null;
    }

    public static void StopAmbience()
    {
        if (instance == null || instance.ambienceSource == null)
        {
            return;
        }

        instance.ambienceSource.Stop();
        instance.ambienceSource.clip = null;
    }

    public static void SetMusicVolume(float value)
    {
        LoadSettings();
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyCurrentSettings();
    }

    public static void SetSfxVolume(float value)
    {
        LoadSettings();
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        ApplyCurrentSettings();
    }

    public static void SetAmbienceVolume(float value)
    {
        LoadSettings();
        ambienceVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, ambienceVolume);
        PlayerPrefs.Save();
        ApplyCurrentSettings();
    }

    public static void SetMusicMuted(bool muted)
    {
        LoadSettings();
        musicMuted = muted;
        PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyCurrentSettings();
    }

    public static void SetSfxMuted(bool muted)
    {
        LoadSettings();
        sfxMuted = muted;
        PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyCurrentSettings();
    }

    public static void SetAmbienceMuted(bool muted)
    {
        LoadSettings();
        ambienceMuted = muted;
        PlayerPrefs.SetInt(AmbienceMutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyCurrentSettings();
    }

    public static void ToggleMusic()
    {
        SetMusicMuted(!MusicMuted);
    }

    public static void SetMusicEnabled(bool enabled)
    {
        SetMusicMuted(!enabled);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject audioObject = new GameObject("GameAudio");
        audioObject.AddComponent<GameAudio>();
    }

    private static void LoadSettings()
    {
        if (settingsLoaded)
        {
            return;
        }

        int savedVersion = PlayerPrefs.GetInt(SettingsVersionKey, 0);

        // Migrate the old music-only setting once.
        if (savedVersion < CurrentSettingsVersion)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, DefaultMusicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, DefaultSfxVolume);
            PlayerPrefs.SetFloat(AmbienceVolumeKey, DefaultAmbienceVolume);
            PlayerPrefs.SetInt(MusicMutedKey, 0);
            PlayerPrefs.SetInt(SfxMutedKey, 0);
            PlayerPrefs.SetInt(AmbienceMutedKey, 0);
            PlayerPrefs.DeleteKey(LegacyMusicEnabledKey);
            PlayerPrefs.SetInt(SettingsVersionKey, CurrentSettingsVersion);
            PlayerPrefs.Save();
        }

        musicVolume = PlayerPrefs.GetFloat(
            MusicVolumeKey,
            DefaultMusicVolume
        );

        sfxVolume = PlayerPrefs.GetFloat(
            SfxVolumeKey,
            DefaultSfxVolume
        );

        ambienceVolume = PlayerPrefs.GetFloat(
            AmbienceVolumeKey,
            DefaultAmbienceVolume
        );

        musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) != 0;
        sfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) != 0;
        ambienceMuted = PlayerPrefs.GetInt(AmbienceMutedKey, 0) != 0;
        settingsLoaded = true;
    }

    private static void ApplyCurrentSettings()
    {
        // Apply saved volume and mute values to the active sources.
        if (instance == null)
        {
            return;
        }

        if (instance.sfxSource != null)
        {
            instance.sfxSource.volume = sfxVolume;
            instance.sfxSource.mute = sfxMuted;
        }

        if (instance.musicSource != null)
        {
            instance.musicSource.volume =
                instance.musicBaseVolume * musicVolume;
            instance.musicSource.mute = musicMuted;
        }

        if (instance.ambienceSource != null)
        {
            instance.ambienceSource.volume =
                instance.ambienceBaseVolume * ambienceVolume;
            instance.ambienceSource.mute = ambienceMuted;
        }
    }

    private static void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }
}
