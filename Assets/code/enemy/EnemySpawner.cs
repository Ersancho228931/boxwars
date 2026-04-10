using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    public float minSpawnTime = 2f;
    public float maxSpawnTime = 4f;

    [Header("Limits")]
    public int maxEnemiesDay = 5;
    public int maxEnemiesNight = 10;
    public int maxDeadBodies = 10;

    private int currentEnemies = 0;
    private List<GameObject> deadBodies = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            int currentMax = DayNightManager.instance.IsDay() ? maxEnemiesDay : maxEnemiesNight;

            if (currentEnemies < currentMax)
            {
                GameObject e = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
                currentEnemies++;

                EnemyHealth eh = e.GetComponent<EnemyHealth>();

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
        }
    }

    public void IncreaseDifficulty(int day)
    {
        maxEnemiesDay += 1;
        maxEnemiesNight += 2; // night scales faster 😈
        maxDeadBodies += 2;
    }
}