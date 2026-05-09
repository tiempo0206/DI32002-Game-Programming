using UnityEngine;

/// <summary>
/// A class which spawns enemies in an area around it.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("GameObject References")]
    [Tooltip("The enemy prefab to use when spawning enemies")]
    public GameObject enemyPrefab = null;

    [Tooltip("The target of the spawned enemies")]
    public Transform target = null;

    [Header("Spawn Position")]
    [Tooltip("The distance within which enemies can spawn in the X direction")]
    [Min(0)] public float spawnRangeX = 10.0f;

    [Tooltip("The distance within which enemies can spawn in the Y direction")]
    [Min(0)] public float spawnRangeY = 10.0f;

    [Header("Spawn Variables")]
    [Tooltip("The maximum number of enemies that can be spawned from this spawner")]
    public int maxSpawn = 20;

    [Tooltip("Ignores the max spawn limit if true")]
    public bool spawnInfinite = true;

    [Tooltip("The time delay between spawning enemies")]
    public float spawnDelay = 2.5f;

    [Tooltip("The object to make projectiles child objects of.")]
    public Transform projectileHolder = null;

    [Header("Difficulty Scaling")]
    [Tooltip("Whether this spawner should get faster as the score rises.")]
    public bool scaleDifficultyWithScore = false;

    [Tooltip("The number of score points required for each difficulty step.")]
    [Min(1)] public int scorePerDifficultyStep = 25;

    [Tooltip("How much spawn delay is reduced at each difficulty step.")]
    [Min(0f)] public float spawnDelayReductionPerStep = 0.15f;

    [Tooltip("The lowest spawn delay this spawner can reach.")]
    [Min(0.1f)] public float minimumSpawnDelay = 0.75f;

    [Tooltip("Extra movement speed applied to spawned enemies per difficulty step.")]
    [Min(0f)] public float enemySpeedBonusPerStep = 0.35f;

    private int currentlySpawned = 0;
    private float lastSpawnTime = Mathf.NegativeInfinity;

    private void Update()
    {
        CheckSpawnTimer();
    }

    private void CheckSpawnTimer()
    {
        float currentSpawnDelay = GetCurrentSpawnDelay();
        if (Time.timeSinceLevelLoad > lastSpawnTime + currentSpawnDelay && (currentlySpawned < maxSpawn || spawnInfinite))
        {
            Vector3 spawnLocation = GetSpawnLocation();
            SpawnEnemy(spawnLocation);
        }
    }

    private void SpawnEnemy(Vector3 spawnLocation)
    {
        if (enemyPrefab == null)
        {
            return;
        }

        GameObject enemyGameObject = Instantiate(enemyPrefab, spawnLocation, enemyPrefab.transform.rotation, null);
        Enemy enemy = enemyGameObject.GetComponent<Enemy>();
        ShootingController[] shootingControllers = enemyGameObject.GetComponentsInChildren<ShootingController>();

        if (enemy != null)
        {
            enemy.followTarget = target;
            enemy.moveSpeed += GetDifficultySteps() * enemySpeedBonusPerStep;
        }

        foreach (ShootingController gun in shootingControllers)
        {
            gun.projectileHolder = projectileHolder;
        }

        currentlySpawned++;
        lastSpawnTime = Time.timeSinceLevelLoad;
    }

    protected virtual Vector3 GetSpawnLocation()
    {
        float x = Random.Range(0 - spawnRangeX, spawnRangeX);
        float y = Random.Range(0 - spawnRangeY, spawnRangeY);
        return new Vector3(transform.position.x + x, transform.position.y + y, 0);
    }

    private int GetDifficultySteps()
    {
        if (!scaleDifficultyWithScore || GameManager.instance == null)
        {
            return 0;
        }

        return Mathf.Max(0, GameManager.score / scorePerDifficultyStep);
    }

    private float GetCurrentSpawnDelay()
    {
        if (!scaleDifficultyWithScore)
        {
            return spawnDelay;
        }

        float adjustedDelay = spawnDelay - (GetDifficultySteps() * spawnDelayReductionPerStep);
        return Mathf.Max(minimumSpawnDelay, adjustedDelay);
    }
}
