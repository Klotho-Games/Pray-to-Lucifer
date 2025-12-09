using System.Collections;
using PrimeTween;
using TMPro;
using UnityEditor.SearchService;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    #region Serialized Fields
    
    [Header("Level & Wave Settings")]
    [SerializeField] private LevelSO[] levels;
    [SerializeField] private bool autoStartWave = true;
    [SerializeField] private SpriteRenderer floorRenderer;
    [SerializeField] private SpriteRenderer levelTransitionOverlayPanel;
    
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
    [SerializeField] private TMP_Text tutorialInstructionText;
    [SerializeField] private Vector2 tutorialEnemySpawnPos;
    [SerializeField] private GameObject TutorialEnemy;
    
    [Header("Player & Camera")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera cam;
    
    [Header("UI Elements")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private float waveTextShowDuration = 3f;
    [SerializeField] private GameObject youDiedScreen;
    [SerializeField] private TMP_Text respawnCounterText;
    [SerializeField] private GameObject gameCompleteScreen;
    [SerializeField] private TMP_Text pressMToResumeText;
    [SerializeField] private string updatedPressMToResumeText;
    [SerializeField] private TMP_Text controlsText;
    [SerializeField] private string controlsCheatSheet;
    
    [Header("Death & Respawn")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private GameObject respawnButton;
    
    [Header("Pooling Settings")]
    [SerializeField] private ObjectPooler objectPooler;
    [SerializeField] private int poolSize = 100;
    [Header("Level Music")]
    // Based on time even tho levels aren't. TODO: fix that later
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip[] musicClips;

   
    #endregion

    #region Private Variables
    
    private PlayerStats playerStats;
    private Collider2D playerCollider;
    private int currentLevelIndex = 0;
    private int currentWaveIndex = 0;
    private int currentTutorialStep = 0;
    
    private bool isPlaying = false;
    private bool isTutorial = false;
    private bool isDead = false;
    
    private Coroutine waveCoroutine;
    private Coroutine startNextWaveCoroutine;
    private Coroutine tutorialEnemyRespawnCoroutine;
    
    #endregion
    
    #region Unity Lifecycle
    
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

    void Start()
    {
        playerStats = playerTransform.GetComponent<PlayerStats>();
        if (autoStartWave && levels.Length > 0 && levels[currentLevelIndex].waves.Length > 0)
        {
            StartWave(currentWaveIndex);
        }
    }

    void Update()
    {
        if (!isPlaying)
            return;

        if (isTutorial)
        {
            TutorialUpdate();
        }
        else
        {
            UpdateGame();
        }

        if (playerStats.CurrentHealth <= 0 && !isDead)
        {
            OnPlayerDeath();
        }
    }
    
    #endregion

    #region Game Flow - Start/Stop

    public void StartGame()
    {
        Debug.Log("Starting Game");
        ChangeMainMenuUI();

        Time.timeScale = 1f;

        isPlaying = true;
        if (isTutorial)
        {
            DisableTutorialElements();
            isTutorial = false;
        }
        currentLevelIndex = 0; // Marking this place for it will be changed if we make a save system of progress to start from the level they finished at
        currentWaveIndex = 0;

        StartLevel(currentLevelIndex);

        void DisableTutorialElements()
        {
            tutorialInstructionText.gameObject.SetActive(false);
        }
    }

    public void StartTutorial()
    {
        Debug.Log("Starting Tutorial");
        ChangeMainMenuUI();

        Time.timeScale = 1f;

        if (!isTutorial)
        {
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }

            if (startNextWaveCoroutine != null)
            {
                StopCoroutine(startNextWaveCoroutine);
                startNextWaveCoroutine = null;
            }
        }

        isPlaying = true;
        isTutorial = true;
        currentTutorialStep = 0;
        tutorialInstructionText.gameObject.SetActive(true);
        GatePlacementManager.instance.DestroyAllPlacedGates();
        DestroyAllEnemiesAndSoulShards();
        RespawnPlayer();
        playerStats.TakeDamage(200);
        tutorialEnemyRespawnCoroutine = StartCoroutine(RespawnTutorialEnemyAfterDelay(0f));
    }
    
    #endregion

    #region Level Management

    public void StartLevel(int levelIndex)
    {
        Debug.Log($"Starting Level {levelIndex + 1}");

        if (levelIndex > 0)
        {
            Time.timeScale = 0f;
            Color color = levelTransitionOverlayPanel.color;
            Tween.Alpha(levelTransitionOverlayPanel, 1f, 1f, Ease.InSine, useUnscaledTime: true).OnComplete(() =>
            {
                Time.timeScale = 1f;
                Tween.Alpha(levelTransitionOverlayPanel, 0f, 2f, Ease.OutSine, useUnscaledTime: true);
            });  
        }

        currentLevelIndex = levelIndex;

        // In case there are less music clips than levels, we take modulo

        //musicSource.clip = musicClips[levelIndex % musicClips.Length];
        //if (!musicSource.isPlaying)
        //{
        //    musicSource.Play();
        //}

        // Complete cleanup
        DestroyAllEnemiesAndSoulShards();
        GatePlacementManager.instance.DestroyAllPlacedGates();

        playerTransform.position = levels[currentLevelIndex].playerSpawnPosition;
        floorRenderer.sprite = levels[currentLevelIndex].floorMap;

        StartWave(0);
    }

    private void ResetLevel()
    {
        Debug.Log($"Resetting Level {currentLevelIndex}");
        DestroyAllEnemiesAndSoulShards();
        StartWave(0);
    }

    #endregion

    #region Wave Management

    public void StartWave(int waveIndex)
    {
    Debug.Log($"Starting Wave {waveIndex + 1} of Level {currentLevelIndex + 1}");

    if (waveCoroutine != null)
        StopCoroutine(waveCoroutine);

    if (waveIndex >= levels[currentLevelIndex].waves.Length)
        waveIndex = levels[currentLevelIndex].waves.Length - 1;

    if (waveIndex < 0)
        waveIndex = 0;

    currentWaveIndex = waveIndex;

    // 🎵 Change music instantly based on wave index
	
    if (musicClips.Length > 0)
    {
    int idx = currentWaveIndex % musicClips.Length;
    musicSource.clip = musicClips[idx];
    musicSource.Play();
    }


    waveCoroutine = StartCoroutine(
        SpawnWaveCoroutine(levels[currentLevelIndex].waves[currentWaveIndex].waveData)
    );

    ShowWaveText();

    void ShowWaveText()
    {
        if (waveIndex == 0)
            waveText.text = $"Level {currentLevelIndex + 1}\nWave {waveIndex + 1}";
        else
            waveText.text = $"Wave {waveIndex + 1}";

        StartCoroutine(ShowForSeconds(waveText.gameObject, waveTextShowDuration));
    }
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
        waveCoroutine = null;
    }

    private IEnumerator StartNextWaveAfterSeconds(float seconds)
    {
        if (startNextWaveCoroutine != null)
            yield break;
        
        yield return new WaitForSeconds(seconds);
        StartWave(currentWaveIndex + 1);
        startNextWaveCoroutine = null;
    }
    
    #endregion

    #region Enemy Spawning

    private void SpawnWeightedEnemy(WaveDataSO wave)
    {
        int totalWeight = 0;
        foreach (var entry in wave.prefabsWithWeights)
            totalWeight += entry.spawnWeight;
        if (totalWeight == 0) return;
        int roll = Random.Range(0, totalWeight);
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
            Vector3 spawnPos = GetPointOutsideCameraView(0f);
            var enemy = objectPooler.GetFromPool(selectedPrefab, spawnPos, transform, poolSize);
            InitializeEnemyAI(enemy);
        }
    }

    private void InitializeEnemyAI(GameObject enemy)
    {
        if (playerCollider == null)
        {
            playerCollider = playerTransform.GetComponent<Collider2D>();
        }
        if (playerStats == null)
        {
            playerStats = playerTransform.GetComponent<PlayerStats>();
        }

        if (enemy.TryGetComponent(out EnergyAI energyAI))
            energyAI.Initialize(playerTransform);
        if (enemy.TryGetComponent(out MeleeAttack meleeAttack))
            meleeAttack.Initialize(playerCollider, playerStats);
    }

    private Vector3 GetPointOutsideCameraView(float groupSize)
    {
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        camWidth += 2f + groupSize;
        camHeight += 2f + groupSize;

        float x, y;
        int side = Random.Range(0, 4);

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
    
    #endregion

    #region Game Update Loop

    private void UpdateGame()
    {
        if (waveCoroutine == null && AllEnemiesAreDefeated())
        {
            Debug.Log($"Wave coroutine is null and all enemies are defeated for Wave {currentWaveIndex + 1} of Level {currentLevelIndex + 1}");
            if (currentWaveIndex + 1 < levels[currentLevelIndex].waves.Length)
            {
                Debug.Log($"Scheduling next wave: Wave {currentWaveIndex + 2} of Level {currentLevelIndex + 1}");
                startNextWaveCoroutine ??= StartCoroutine(StartNextWaveAfterSeconds(levels[currentLevelIndex].waves[currentWaveIndex + 1].delayAfterPreviousWave));
            }
            else
            {
                if (currentLevelIndex + 1 < levels.Length)
                {
                    StartLevel(currentLevelIndex + 1);
                }
                else
                {
                    OnGameComplete();
                }
            }
        }
    }

    private bool AllEnemiesAreDefeated()
    {
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Enemy"))
                return false;
        }

        return true;
    }

    private void OnGameComplete()
    {
        gameCompleteScreen.SetActive(true);
    }
    
    #endregion

    #region Tutorial

    private bool TutorialUpdate()
    {
        if (tutorialEnemyRespawnCoroutine == null && transform.childCount == 0)
        {
            tutorialEnemyRespawnCoroutine = StartCoroutine(RespawnTutorialEnemyAfterDelay(1f));
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
                if (transform.childCount > 0 && transform.GetChild(0).GetComponent<SoulShard>() != null)
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
                if (GatePlacementManager.instance.HasDestroyedGate)
                {
                    currentTutorialStep++;
                }
                break;
            default:
                break;
        }
        return true;
    }

    IEnumerator RespawnTutorialEnemyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TutorialEnemy.SetActive(true);
        TutorialEnemy.transform.parent = transform;

        EnemyLifeSystem lifeSystem = TutorialEnemy.GetComponent<EnemyLifeSystem>();
        lifeSystem.ResetHealth();
        
        InitializeEnemyAI(TutorialEnemy);
        
        tutorialEnemyRespawnCoroutine = null;
    }
    
    #endregion

    #region Death & Respawn

    private IEnumerator RespawnCountdown()
    {
        float countdown = 0;

        while (countdown < respawnDelay)
        {
            respawnCounterText.text = $"Respawning in {Mathf.CeilToInt(respawnDelay - countdown)}";
            yield return null;
            countdown += Time.unscaledDeltaTime;
        }

        respawnButton.SetActive(true);
        respawnCounterText.text = "Click to Respawn";
    }

    public void OnPlayerDeath()
    {
        isDead = true;
        youDiedScreen.SetActive(true);
        ProjectManager.instance.StopTime();
        StartCoroutine(RespawnCountdown());
        // more player death handling logic can be added here
    }

    public void RespawnPlayer()
    {
        playerStats.ResetCurrentHealth();
        youDiedScreen.SetActive(false);
        isDead = false;
        Time.timeScale = 1f;
        if (isTutorial)
        {
            tutorialEnemyRespawnCoroutine = StartCoroutine(RespawnTutorialEnemyAfterDelay(0f));
        }
        else
        {
            ResetLevel();
        }
    }
    
    #endregion

    #region Utility Methods

    private void DestroyAllEnemiesAndSoulShards()
    {
        foreach (var enemy in FindObjectsByType<EnemyLifeSystem>(FindObjectsSortMode.None))
        {
            if (enemy.gameObject.activeSelf)
            {
                enemy.gameObject.SetActive(false);
                objectPooler.ReturnToPool(enemy.gameObject, enemy.gameObject);
            }
        }
        foreach (var shard in FindObjectsByType<SoulShard>(FindObjectsSortMode.None))
        {
            if (shard.gameObject.activeSelf)
            {
                shard.gameObject.SetActive(false);
                objectPooler.ReturnToPool(shard.gameObject, shard.gameObject);
            }
        }
    }

    private IEnumerator ShowForSeconds(GameObject obj, float seconds)
    {
        obj.SetActive(true);
        yield return new WaitForSeconds(seconds);
        obj.SetActive(false);
    }

    private void ChangeMainMenuUI()
    {
        pressMToResumeText.text = updatedPressMToResumeText;
        controlsText.text = controlsCheatSheet;
    }
    
    #endregion
}
