using UnityEngine;

public class LevelAndRespawnManager : MonoBehaviour
{
    public static LevelAndRespawnManager instance;
    private int currentLevelIndex = 0;
    private float levelTimer = 0f;
    private int currentTimelineElementIndex = 0;
    [SerializeField] private LevelTimeline[] levels;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera cam;
    private PlayerStats playerStats;
    
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
        RespawnPlayer();
    }

    void Update()
    {
        if (playerStats.CurrentHealth <= 0)
        {
            OnPlayerDeath();
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
                SpawnEnemyScattered(enemyGroup.enemyPrefab);
            }
        }
        else
        {
            // Use a clutter at a random position outside the camera view
        }
    }

    private void SpawnEnemyScattered(GameObject enemyToSpawn)
    {
        Vector3 spawnPosition = GetPointOutsideCameraView();
        EnergyMeleeAI AI = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity, transform).GetComponent<EnergyMeleeAI>();
        AI.Initialize(playerTransform);
    }

    private Vector3 GetPointOutsideCameraView()
    {
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        camWidth += 2f; // Extra buffer
        camHeight += 2f; // Extra buffer

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
        if (currentLevelIndex < levels.Length)
        {

            ++currentLevelIndex;
        }
        else
        {
            Debug.Log("All levels completed!");
            // Handle end of game logic here
        }
    }

    public void OnPlayerDeath()
    {
        // Handle player death and respawn logic here
        Debug.Log("Player has died. Respawning...");
        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        playerTransform.position = levels[currentLevelIndex].spawnPosition;
    }
}
