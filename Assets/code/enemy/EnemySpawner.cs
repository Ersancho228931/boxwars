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
        public int spawnDay = 1;
        public int spawnCount = 1;
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

    void Start()
    {
        StartCoroutine(SpawnLoop());
        if (DayNightManager.instance != null)
            lastDay = DayNightManager.instance.GetDay();
    }

    IEnumerator SpawnLoop()
    {
        // continuous background loop to enforce general spawning if entries are none
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));
            int currentMax = (DayNightManager.instance != null && DayNightManager.instance.IsDay()) ? maxEnemiesDay : maxEnemiesNight;
            if (currentEnemies < currentMax && spawnEntries.Count == 0)
            {
                // fallback simple spawn of single prefab if user left old field empty
                // do nothing here by default
            }

            // check day change and trigger per-day spawns
            if (DayNightManager.instance != null)
            {
                int day = DayNightManager.instance.GetDay();
                bool isDay = DayNightManager.instance.IsDay();
                if (isDay && day != lastDay)
                {
                    lastDay = day;
                    OnDayStart(day);
                }
            }
        }
    }

    void OnDayStart(int day)
    {
        // start spawn routines for entries that match this day
        foreach (var entry in spawnEntries)
        {
            if (entry != null && entry.spawnDay == day && entry.prefab != null)
            {
                StartCoroutine(SpawnEntryRoutine(entry));
            }
        }
    }

    IEnumerator SpawnEntryRoutine(SpawnEntry entry)
    {
        for (int i = 0; i < entry.spawnCount; i++)
        {
            // wait a bit between spawns, random within entry interval
            float wait = Random.Range(entry.spawnIntervalMin, entry.spawnIntervalMax);
            yield return new WaitForSeconds(wait);

            // wait until there's room according to current limits
            int tries = 0;
            while (currentEnemies >= ((DayNightManager.instance != null && DayNightManager.instance.IsDay()) ? maxEnemiesDay : maxEnemiesNight))
            {
                yield return new WaitForSeconds(0.5f);
                tries++;
                if (tries > 20) break; // avoid infinite hang
            }

            // spawn if still allowed
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