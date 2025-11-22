using System.Collections;
using System.Collections.Generic;
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
    [Header("Wave Settings")]
    [SerializeField] private List<WaveDataSO> waves;
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool autoStartWave = true;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera cam;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private GameObject youDiedScreen;
    [SerializeField] private GameObject nextlevelScreen;
    [SerializeField] private float levelCompleteScreenDuration = 3f;
    [SerializeField] private GameObject gameCompleteScreen;
    [Header("Pooling Settings")]
    [SerializeField] private ObjectPooler objectPooler;
    [SerializeField] private int poolSize = 100;
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
        if (autoStartWave && waves.Count > 0)
        {
            StartWave(currentWaveIndex);
        }
    }
    private Coroutine waveCoroutine;

    public void StartWave(int waveIndex)
    {
        if (waveCoroutine != null)
            StopCoroutine(waveCoroutine);
        if (waveIndex < 0 || waveIndex >= waves.Count)
            return;
    waveCoroutine = StartCoroutine(SpawnWaveCoroutine(waves[waveIndex]));
    }

    private IEnumerator SpawnWaveCoroutine(WaveDataSO wave)
    {
        float timer = 0f;
        float interval = wave.startingSpawnInterval;
        while (timer < wave.waveDuration)
        {
            SpawnWeightedEnemy(wave);
            yield return new WaitForSeconds(interval);
            timer += interval;
            interval = Mathf.Max(wave.minSpawnInterval, interval - wave.spawnIntervalDecrement);
        }
    }

    private void SpawnWeightedEnemy(WaveDataSO wave)
    {
        int totalWeight = 0;
        foreach (var entry in wave.prefabsWithWeights)
            totalWeight += entry.spawnWeight;
        if (totalWeight == 0) return;
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        GameObject selectedPrefab = null;
        foreach (var entry in wave.prefabsWithWeights)
        {
            cumulative += entry.spawnWeight;
            if (roll < cumulative)
            {
                selectedPrefab = entry.prefab;
                break;
            }
        }
        if (selectedPrefab != null)
        {
            Vector3 spawnPos = GetPointOutsideCameraView(0f); // You can adjust spawn logic as needed
            var enemy = objectPooler.GetFromPool(selectedPrefab, spawnPos);
            InitializeEnemyAI(enemy);
        }
    }

    private GameObject GetFromPool(GameObject prefab, Vector3 position)
    {
        return objectPooler.GetFromPool(prefab, position);
    }

    public void ReturnToPool(GameObject prefab, GameObject obj)
    {
        objectPooler.ReturnToPool(prefab, obj);
    }

    // private void SetActiveTimelines(LevelTimeline[] timelines) { }

    // Timeline-based spawning coroutine removed. Will be replaced by WaveData logic.

    // Timeline-based level start removed. Will be replaced by WaveData logic.

    public void StartGame()
    {
        isTutorial = false;
        currentWaveIndex = 0;
        endTutorialButton.SetActive(false);
        tutorialInstructionText.gameObject.SetActive(false);
        GatePlacementManager.instance.DestroyAllPlacedGates();
        DestroyAllEnemiesAndSoulShards();
        StartCoroutine(ShowForSeconds(nextlevelScreen, levelCompleteScreenDuration));
        RespawnPlayer();
        StartWave(currentWaveIndex);
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
        if (playerStats.CurrentHealth <= 0 && !isDead)
        {
            OnPlayerDeath();
        }
        return true;
    }

    private void InitializeEnemyAI(GameObject enemy)
    {
        if (enemy.TryGetComponent(out EnergyMeleeAI energyMeleeAI))
            energyMeleeAI.Initialize(playerTransform);
        else if (enemy.TryGetComponent(out MaterialMeleeAI materialMeleeAI))
            materialMeleeAI.Initialize(playerTransform);
        else if (enemy.TryGetComponent(out MaterialProjectileAI materialProjectileAI))
            materialProjectileAI.Initialize(playerTransform);
        else if (enemy.TryGetComponent(out MaterialGrenadeAI materialGrenadeAI))
            materialGrenadeAI.Initialize(playerTransform);
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

    // Timeline-based level loading removed. Use wave-based progression if needed.

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
    playerTransform.position = Vector3.zero; // Set to default or configurable spawn position
    playerStats.ResetCurrentHealth();
    youDiedScreen.SetActive(false);
    isDead = false;
    }

    private void DestroyAllEnemiesAndSoulShards()
    {
        foreach (var obj in GameObject.FindObjectsOfType<EnemyLifeSystem>())
        {
            if (obj.gameObject.activeSelf)
            {
                obj.gameObject.SetActive(false);
                objectPooler.ReturnToPool(obj.gameObject, obj.gameObject);
            }
        }
    }
}
