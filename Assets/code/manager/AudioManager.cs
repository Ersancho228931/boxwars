using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Clips")]
    public AudioClip shooterShoot;
    public AudioClip playerWalk;
    public AudioClip playerWalkInjured; // New: Louder/Different walk
    public AudioClip enemyBreak;
    public AudioClip dayNightChange;
    public AudioClip bomberExplosion;
    public AudioClip bossSpawn; // New: Boss Spawn sound

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    private AudioSource walkSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        walkSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayOneShot(AudioClip clip, float vol = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, vol);
    }

    public void PlayWalk(bool lowHealth, float vol = 0.8f)
    {
        // If low health, use injured clip and boost volume
        AudioClip clipToPlay = lowHealth ? playerWalkInjured : playerWalk;
        float finalVol = lowHealth ? vol * 1.5f : vol;

        if (clipToPlay == null) return;
        walkSource.PlayOneShot(clipToPlay, Mathf.Clamp01(finalVol));
    }

    public void StopWalk()
    {
        if (walkSource != null) walkSource.Stop();
    }

    public void PlayBossSpawn() => PlayOneShot(bossSpawn, 1f);
}