using TMPro;
using UnityEngine;

public class DayNightManager : MonoBehaviour
{
    public float dayDuration = 45f;
    public float nightDuration = 30f;
    public TMP_Text infoText;
    public GameObject nightOverlay;

    [Header("Night 5 Cleanup")]
    public bool clearDeadBodiesOnNight5 = true;   // ← Toggle this in Inspector

    private float timer;
    private bool isDay = true;
    private int dayCount = 1;
    public static DayNightManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        timer = dayDuration;
        UpdateUI();
    }

    void Update()
    {
        if (!isDay && dayCount >= 5)
        {
            return; // Forever night - stop timer
        }

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SwitchPhase();
        }
    }

    void SwitchPhase()
    {
        AudioManager.Instance.PlayOneShot(AudioManager.Instance.dayNightChange, 1.5f);
        isDay = !isDay;

        if (isDay)
        {
            dayCount++;
            timer = dayDuration;

            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
                spawner.IncreaseDifficulty(dayCount);
        }
        else
        {
            timer = nightDuration;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        string phase = isDay ? "DAY" : "NIGHT";

        if (!isDay && dayCount >= 5)
        {
            infoText.text = "Day: " + dayCount + "\nFOREVER NIGHT";
        }
        else
        {
            infoText.text = "Day: " + dayCount + "\n" + phase;
        }

        if (nightOverlay != null)
            nightOverlay.SetActive(!isDay);
    }

    public bool IsDay() => isDay;
    public int GetDay() => dayCount;
}