using UnityEngine;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX")]
    public AudioClip shooterShoot;
    public AudioClip playerWalk;
    public AudioClip enemyBreak;
    public AudioClip dayNightChange;
    public AudioClip bomberExplosion;

    [Header("Music")]
    public AudioClip backgroundMusic; // основная музыка уровня
    public AudioClip bossMusic;       // опционально можно задать глобально
    public AudioSource musicSource;
    public AudioSource sfxSource; // one-shot SFX

    private AudioClip previousMusicClip; // для восстановления после босса

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
    }

    public void PlayOneShot(AudioClip clip, float vol = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, vol);
    }

    public void PlayMusic(AudioClip clip, float vol = 1f)
    {
        if (musicSource == null) return;
        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }
        musicSource.clip = clip;
        musicSource.volume = vol;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    // convenience: play background music assigned in inspector
    public void PlayBackgroundMusic(float vol = 1f)
    {
        if (backgroundMusic == null) return;
        PlayMusic(backgroundMusic, vol);
    }

    // start boss music and remember previous clip for restore
    public void PlayBossMusic(AudioClip bossClip, float vol = 1f)
    {
        if (musicSource == null) return;
        // save current clip (could be backgroundMusic or something else)
        previousMusicClip = musicSource.clip;
        // if caller didn't provide a boss clip, try global bossMusic field
        AudioClip toPlay = bossClip != null ? bossClip : bossMusic;
        if (toPlay == null) return;
        PlayMusic(toPlay, vol);
    }

    // restore previous music (or backgroundMusic if previous is null)
    public void RestorePreviousMusic(float vol = 1f)
    {
        if (previousMusicClip != null)
        {
            PlayMusic(previousMusicClip, vol);
            previousMusicClip = null;
            return;
        }
        // fallback to background music
        PlayBackgroundMusic(vol);
    }
}