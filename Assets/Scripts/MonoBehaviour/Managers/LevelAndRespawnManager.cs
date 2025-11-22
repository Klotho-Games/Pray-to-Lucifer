using System.Collections;
using UnityEngine;

public class LevelAndRespawnManager : MonoBehaviour
{
    public static LevelAndRespawnManager instance;
    private int currentLevelIndex = 0;
    private float levelTimer = 0f;
    private int currentElementIndex = 0;
    [Header("Tutorial Settings")]
    [SerializeField] private string[] tutorialInstructions = new string[]
    {
        "Use WASD to move around.",
        "Shoot the enemy with your mouse.",
        "Collect the <i>Soul Shard</i> dropped by the enemy to increase your Soul amount (slider in the top-left corner).",
        "Enter <i>Soul State</i> and charge to fully heal (ignore weird behaviour on E or Right Mouse Button, it's under dev).",
        "Place a <i>Densoul</i>: collect enough Soul; press Ctrl; click location; click again to place rotated accordingly.",
        "Press Q or Tab to switch Densoul type (the selected type is displayed in the top-right corner)."
    };
    /*
    0 WASD move, velocity check => next; 
    1 spawn unmoving enemy, Mouse to shoot, destroyed => next
    2 collect the soul shard => next (for all the following steps enemy respawns one sec after soul shard is collected)
    3 enter soul state and charge, fully healed => next
    4 place a densoul => next
    5 input Q or Tab => next
    6 show a button that plays game with text "End tutorial" => open main menu */
    [SerializeField] private TMPro.TMP_Text tutorialInstructionText;
    [SerializeField] private GameObject endTutorialButton;
    [SerializeField] private Vector2 tutorialEnemySpawnPos;
    [SerializeField] private GameObject TutorialEnemyPrefab;
    private int currentTutorialStep = 0;
    private bool tutorialEnemyIsRespawning = false;
    private Transform ChildLastFrame;
    [Header("Level Settings")]
    [SerializeField] private LevelTimeline[] levels;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera cam;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private GameObject youDiedScreen;
    [SerializeField] private GameObject nextlevelScreen;
    [SerializeField] private float levelCompleteScreenDuration = 3f;
    [SerializeField] private GameObject gameCompleteScreen;
    private PlayerStats playerStats;
    private bool isTutorial = false;
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
    }

    public void StartGame()
    {
        isTutorial = false;
        currentLevelIndex = 0;
        levelTimer = 0f;
        currentElementIndex = 0;
        endTutorialButton.SetActive(false);
        tutorialInstructionText.gameObject.SetActive(false);
        GatePlacementManager.instance.DestroyAllPlacedGates();
        DestroyAllEnemiesAndSoulShards();
        StartCoroutine(ShowForSeconds(nextlevelScreen, levelCompleteScreenDuration));
        RespawnPlayer();
    }

    public void StartTutorial()
    {
        isTutorial = true;
        currentTutorialStep = 0;
        tutorialInstructionText.gameObject.SetActive(true);
        GatePlacementManager.instance.DestroyAllPlacedGates();
        DestroyAllEnemiesAndSoulShards();
        currentLevelIndex = 0;
        RespawnPlayer();
        playerStats.TakeDamage(200);
    }

    void Update()
    {
        if (isTutorial)
        {
            TutorialUpdate();
        }
        else
        {
            bool flowControl = GameUpdate();
            if (!flowControl)
            {
                return;
            }
        }
    }

    private bool TutorialUpdate()
    {
        if (!tutorialEnemyIsRespawning && transform.childCount == 0)
        {
            StartCoroutine(RespawnTutorialEnemyAfterDelay(1f));
        }

        if (currentTutorialStep >= 0 && currentTutorialStep < tutorialInstructions.Length)
        {
            tutorialInstructionText.text = tutorialInstructions[currentTutorialStep];
        }
        else
        {
            tutorialInstructionText.gameObject.SetActive(false);
        }

        switch (currentTutorialStep)
        {
            case 0:
                if (playerStats.GetComponent<Rigidbody2D>().linearVelocity.magnitude > 0.1f)
                {
                    currentTutorialStep++;
                }
                break;
            case 1:
                if (ChildLastFrame != transform.GetChild(0))
                {
                    currentTutorialStep++;
                }
                break;
            case 2:
                if (transform.childCount == 0)
                {
                    currentTutorialStep++;
                }
                break;
            case 3:
                if (playerStats.CurrentHealth >= playerStats.MaxHealth)
                {
                    currentTutorialStep++;
                }
                break;
            case 4:
                if (GatePlacementManager.instance.HasPlacedGate)
                {
                    currentTutorialStep++;
                }
                break;
            case 5:
                if (InputManager.instance.ItemRightAction.IsPressed() || InputManager.instance.ItemLeftAction.IsPressed())
                {
                    currentTutorialStep++;
                }
                break;
            case 6:
                endTutorialButton.SetActive(true);
                currentTutorialStep++; // Move past tutorial to prevent re-trigger
                break;
            default:
                break;
        }
        return true;
    }

    IEnumerator RespawnTutorialEnemyAfterDelay(float delay)
    {
        tutorialEnemyIsRespawning = true;
        yield return new WaitForSeconds(delay);
        Instantiate(TutorialEnemyPrefab, (Vector3)tutorialEnemySpawnPos, Quaternion.identity, transform);
        tutorialEnemyIsRespawning = false;
    }

    private bool GameUpdate()
    {
        // check for player death
        if (playerStats.CurrentHealth <= 0 && !isDead)
        {
            OnPlayerDeath();
            if (currentLevelIndex >= levels.Length)
                currentLevelIndex = levels.Length - 1;
            levelTimer = 0f;
            currentElementIndex = 0;
        }

        // check for game completion
        if (currentLevelIndex >= levels.Length)
        {
            if (transform.childCount == 0 && !isDead)
            {
                Time.timeScale = 0f;
                gameCompleteScreen.SetActive(true);
            }
            return false;
        }

        // Level progression
        levelTimer += Time.deltaTime;
        while (transform.childCount <= 0 || levelTimer >= levels[currentLevelIndex].enemyGroups[currentElementIndex].spawnTime)
        {
            levelTimer = levels[currentLevelIndex].enemyGroups[currentElementIndex].spawnTime;
            SpawnEnemyGroup(levels[currentLevelIndex].enemyGroups[currentElementIndex]);
            ++currentElementIndex;

            if (currentElementIndex >= levels[currentLevelIndex].enemyGroups.Count)
            {
                LoadNextLevel();
                currentElementIndex = 0;
                levelTimer = 0f;
                break;
            }
        }

        return true;
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
        var enemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity, transform);
        if (enemy.TryGetComponent(out EnergyMeleeAI energyMeleeAI))
            energyMeleeAI.Initialize(playerTransform);
        else if (enemy.TryGetComponent(out MaterialMeleeAI materialMeleeAI))
            materialMeleeAI.Initialize(playerTransform);
        else if (enemy.TryGetComponent(out MaterialProjectileAI materialProjectileAI))
            materialProjectileAI.Initialize(playerTransform);
        else if (enemy.TryGetComponent(out MaterialGrenadeAI materialGrenadeAI))
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
        ++currentLevelIndex;
        if (currentLevelIndex < levels.Length)
        {
            nextlevelScreen.GetComponent<TMPro.TextMeshProUGUI>().text = "Level " + (currentLevelIndex + 1);
            StartCoroutine(ShowForSeconds(nextlevelScreen, levelCompleteScreenDuration));
            // more level transition logic can be added here
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
        DestroyAllEnemiesAndSoulShards();
        playerTransform.position = levels[currentLevelIndex].spawnPosition;
        playerStats.ResetCurrentHealth();
        youDiedScreen.SetActive(false);
        isDead = false;
    }

    private void DestroyAllEnemiesAndSoulShards()
    {
        foreach (Transform enemyOrShard in transform)
        {
            Destroy(enemyOrShard.gameObject);
        }
    }
}
