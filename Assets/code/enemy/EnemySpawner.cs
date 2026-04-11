using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public string typeName;
        public GameObject prefab;

        [Header("Day range")]
        public bool spawnEveryDay = false;      // если true — спавн будет каждый день
        public int spawnDayMin = 1;            // минимальный день (включительно)
        public int spawnDayMax = 1;            // максимальный день (включительно)

        [Header("Counts")]
        public int spawnCountDay = 1;          // сколько спавнить при старте ДНЯ
        public int spawnCountNight = 0;        // сколько спавнить при старте НОЧИ (0 = не спавним ночью)

        [Header("Timing per entry")]
        public float spawnIntervalMin = 0.5f;
        public float spawnIntervalMax = 1.5f;
    }

    [Header("Global spawn limits")]
    public int maxEnemiesDay = 5;
    public int maxEnemiesNight = 10;
    public int maxDeadBodies = 10;

    [Header("Default spawn timing (fallback)")]
    public float minSpawnTime = 2f;
    public float maxSpawnTime = 4f;

    [Header("Spawn entries per level/day")]
    public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

    private int currentEnemies = 0;
    private List<GameObject> deadBodies = new List<GameObject>();

    private int lastDay = -1;
    private bool lastIsDay = false;

    void Start()
    {
        StartCoroutine(SpawnLoop());
        if (DayNightManager.instance != null)
        {
            lastDay = DayNightManager.instance.GetDay();
            lastIsDay = DayNightManager.instance.IsDay();
            // Если сцена уже в дне/ночи — запустить соответствующие спавны сразу
            if (lastIsDay)
                OnDayStart(lastDay);
            else
                OnNightStart(lastDay);
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            // общая проверка лимитов — используется внутри конкретных рутин спавна
            // отслеживаем смену фазы (день/ночь)
            if (DayNightManager.instance != null)
            {
                int day = DayNightManager.instance.GetDay();
                bool isDay = DayNightManager.instance.IsDay();

                if (day != lastDay || isDay != lastIsDay)
                {
                    // фаза/день изменились
                    lastDay = day;

                    if (isDay && !lastIsDay)
                    {
                        // переход: ночь -> день
                        OnDayStart(day);
                    }
                    else if (!isDay && lastIsDay)
                    {
                        // переход: день -> ночь
                        OnNightStart(day);
                    }

                    lastIsDay = isDay;
                }
            }
        }
    }

    void OnDayStart(int day)
    {
        foreach (var entry in spawnEntries)
        {
            if (entry == null || entry.prefab == null) continue;

            if (entry.spawnEveryDay || (day >= entry.spawnDayMin && day <= entry.spawnDayMax))
            {
                int count = Mathf.Max(0, entry.spawnCountDay);
                if (count > 0)
                    StartCoroutine(SpawnEntryRoutine(entry, count));
            }
        }
    }

    void OnNightStart(int day)
    {
        foreach (var entry in spawnEntries)
        {
            if (entry == null || entry.prefab == null) continue;

            if (entry.spawnEveryDay || (day >= entry.spawnDayMin && day <= entry.spawnDayMax))
            {
                int count = Mathf.Max(0, entry.spawnCountNight);
                if (count > 0)
                    StartCoroutine(SpawnEntryRoutine(entry, count));
            }
        }
    }

    IEnumerator SpawnEntryRoutine(SpawnEntry entry, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float wait = Random.Range(entry.spawnIntervalMin, entry.spawnIntervalMax);
            yield return new WaitForSeconds(wait);

            // wait until there's room according to current day/night limits
            int tries = 0;
            while (currentEnemies >= ((DayNightManager.instance != null && DayNightManager.instance.IsDay()) ? maxEnemiesDay : maxEnemiesNight))
            {
                yield return new WaitForSeconds(0.5f);
                tries++;
                if (tries > 40) break; // safety break
            }

            if (currentEnemies < ((DayNightManager.instance != null && DayNightManager.instance.IsDay()) ? maxEnemiesDay : maxEnemiesNight))
            {
                GameObject e = Instantiate(entry.prefab, transform.position, Quaternion.identity);
                currentEnemies++;

                EnemyHealth eh = e.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.OnDeath += () =>
                    {
                        currentEnemies--;

                        // Track dead bodies
                        deadBodies.Add(e);

                        if (deadBodies.Count > maxDeadBodies)
                        {
                            if (deadBodies[0] != null)
                                Destroy(deadBodies[0]);

                            deadBodies.RemoveAt(0);
                        }
                    };
                }
                else
                {
                    // fallback: if no EnemyHealth, still decrement on destroy after lifetime
                    Destroy(e, 60f);
                }
            }
        }
    }

    public void IncreaseDifficulty(int day)
    {
        maxEnemiesDay += 1;
        maxEnemiesNight += 2; // night scales faster 😈
        maxDeadBodies += 2;
    }
}