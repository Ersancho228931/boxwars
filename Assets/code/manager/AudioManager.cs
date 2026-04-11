using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Clips")]
    public AudioClip shooterShoot;
    public AudioClip playerWalk;
    public AudioClip playerWalkInjured;
    public AudioClip enemyBreak;
    public AudioClip dayNightChange;
    public AudioClip bomberExplosion;
    public AudioClip bossSpawn;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    private AudioSource walkSource;

    void Awake()
    {
        // Safe singleton: always use the new instance after scene reload
        Instance = this;

        // Setup sources if missing
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        if (walkSource == null)
            walkSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayOneShot(AudioClip clip, float vol = 1f)
    {
        if (clip == null || sfxSource == null) return;   // ← Prevents the crash
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(vol));
    }

    public void PlayWalk(bool lowHealth, float vol = 0.8f)
    {
        AudioClip clipToPlay = lowHealth ? playerWalkInjured : playerWalk;
        if (clipToPlay == null || walkSource == null) return;

        float finalVol = lowHealth ? vol * 1.5f : vol;
        walkSource.PlayOneShot(clipToPlay, Mathf.Clamp01(finalVol));
    }

    public void StopWalk()
    {
        if (walkSource != null)
            walkSource.Stop();
    }

    public void PlayBossSpawn() => PlayOneShot(bossSpawn, 1f);
}