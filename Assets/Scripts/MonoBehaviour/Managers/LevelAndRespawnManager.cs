using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class LevelAndRespawnManager : MonoBehaviour
{
    public static LevelAndRespawnManager instance;
    private int currentLevelIndex = 0;
    private float levelTimer = 0f;
    private int currentTimelineElementIndex = 0;
    [SerializeField] private LevelTimeline[] levels;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera cam;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private GameObject youDiedScreen;
    [SerializeField] private GameObject nextlevelScreen;
    [SerializeField] private float levelCompleteScreenDuration = 3f;
    [SerializeField] private GameObject gameCompleteScreen;
    private PlayerStats playerStats;
    private bool isDead = false;
    
    #region Instance
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    void Start()
    {
        playerStats = playerTransform.GetComponent<PlayerStats>();
        StartCoroutine(ShowForSeconds(nextlevelScreen, levelCompleteScreenDuration));
        RespawnPlayer();
    }

    void Update()
    {
        if (playerStats.CurrentHealth <= 0 && !isDead)
        {
            OnPlayerDeath();
            levelTimer = 0f;
        }

        levelTimer += Time.deltaTime;
        while (transform.childCount <= 0 || levelTimer >= levels[currentLevelIndex].enemyGroups[currentTimelineElementIndex].spawnTime)
        {
            levelTimer = levels[currentLevelIndex].enemyGroups[currentTimelineElementIndex].spawnTime;
            SpawnEnemyGroup(levels[currentLevelIndex].enemyGroups[currentTimelineElementIndex]);
            currentTimelineElementIndex++;

            if (currentTimelineElementIndex >= levels[currentLevelIndex].enemyGroups.Count)
            {
                LoadNextLevel();
                currentTimelineElementIndex = 0;
                levelTimer = 0f;
            }
        }
    }

    private void SpawnEnemyGroup(LevelTimeline.EnemyGroup enemyGroup)
    {
        if (enemyGroup.scattered)
        {
            for (int i = 0; i < enemyGroup.quantity; i++)
            {
                Vector3 spawnPosition = GetPointOutsideCameraView(0f);
                SpawnEnemy(enemyGroup.enemyPrefab, spawnPosition);
            }
        }
        else
        {
            Vector3 groupCenter = GetPointOutsideCameraView(enemyGroup.groupRadius);
            for (int i = 0; i < enemyGroup.quantity; i++)
            {
                Vector2 spawnOffset = Random.insideUnitCircle * enemyGroup.groupRadius;
                Vector3 spawnPosition = new(groupCenter.x + spawnOffset.x, groupCenter.y + spawnOffset.y, 0f);
                SpawnEnemy(enemyGroup.enemyPrefab, spawnPosition);
            }
        }
    }

    private void SpawnEnemy(GameObject enemyToSpawn, Vector3 spawnPosition)
    {
        if (Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity, transform).TryGetComponent(out EnergyMeleeAI energyMeleeAI))
            energyMeleeAI.Initialize(playerTransform);
        else if (Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity, transform).TryGetComponent(out MaterialMeleeAI materialMeleeAI))
            materialMeleeAI.Initialize(playerTransform);
        else if (Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity, transform).TryGetComponent(out MaterialProjectileAI materialProjectileAI))
            materialProjectileAI.Initialize(playerTransform);
        else if (Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity, transform).TryGetComponent(out MaterialGrenadeAI materialGrenadeAI))
            materialGrenadeAI.Initialize(playerTransform);
        else
            Debug.LogError("Enemy prefab does not have a recognized AI component.");
    }

    private Vector3 GetPointOutsideCameraView(float groupSize)
    {
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        camWidth += 2f + groupSize; // Extra buffer
        camHeight += 2f + groupSize; // Extra buffer

        float x, y;
        int side = Random.Range(0, 4); // 0: top, 1: bottom, 2: left, 3: right

        switch (side)
        {
            case 0: // Top
                x = Random.Range(cam.transform.position.x - camWidth / 2, cam.transform.position.x + camWidth / 2);
                y = cam.transform.position.y + camHeight / 2 + 1f;
                break;
            case 1: // Bottom
                x = Random.Range(cam.transform.position.x - camWidth / 2, cam.transform.position.x + camWidth / 2);
                y = cam.transform.position.y - camHeight / 2 - 1f;
                break;
            case 2: // Left
                x = cam.transform.position.x - camWidth / 2 - 1f;
                y = Random.Range(cam.transform.position.y - camHeight / 2, cam.transform.position.y + camHeight / 2);
                break;
            case 3: // Right
                x = cam.transform.position.x + camWidth / 2 + 1f;
                y = Random.Range(cam.transform.position.y - camHeight / 2, cam.transform.position.y + camHeight / 2);
                break;
            default:
                x = 0f;
                y = 0f;
                break;
        }

        return new Vector3(x, y, 0f);
    }

    private void LoadNextLevel()
    {
        if (currentLevelIndex < levels.Length - 1)
        {
            ++currentLevelIndex;
            nextlevelScreen.GetComponent<TMPro.TextMeshProUGUI>().text = "Level " + (currentLevelIndex + 1);
            StartCoroutine(ShowForSeconds(nextlevelScreen, levelCompleteScreenDuration));
            // more level transition logic can be added here
        }
        else if (transform.childCount <= 0)
        {
            Debug.Log("All levels completed!");
            gameCompleteScreen.SetActive(true);
            // more end-of-game logic can be added here
        }
    }

    private IEnumerator ShowForSeconds(GameObject obj, float seconds)
    {
        obj.SetActive(true);
        yield return new WaitForSeconds(seconds);
        obj.SetActive(false);
    }

    public void OnPlayerDeath()
    {
        isDead = true;
        youDiedScreen.SetActive(true);
        StartCoroutine(WaitAndRespawnPlayer());
        // more player death handling logic can be added here
    }

    private IEnumerator WaitAndRespawnPlayer()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(respawnDelay);
        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        Time.timeScale = 1f;
        DestroyAllEnemies();
        playerTransform.position = levels[currentLevelIndex].spawnPosition;
        playerStats.CurrentHealth = playerStats.MaxHealth;
        youDiedScreen.SetActive(false);
        isDead = false;
    }

    private void DestroyAllEnemies()
    {
        foreach (Transform enemy in transform)
        {
            Destroy(enemy.gameObject);
        }
    }
}
