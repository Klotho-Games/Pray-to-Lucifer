using System.Collections.Generic;
using UnityEngine;

public class GatePlacementManager : MonoBehaviour
{
    public static GatePlacementManager instance;

    public enum GateType { DivergingLens, ConvergingLens, Mirror, OneWayMirror, Diffraction }

    [Tooltip("The type of gate to place when in placement mode and for which indicators are shown when cost is coverable")]
    public GateType currentGateType = GateType.Mirror;
    [Header("Do not reorder these prefabs, they correspond to GateType enum")]
    [SerializeField] private List<GameObject> gatePrefabs = new(); // List to hold gate prefabs
    [SerializeField] private Grid hexGrid;
    [SerializeField] private GameObject placementIndicatorPrefab;
    [Range(0,1)][SerializeField] private float slowmoDuringRotation = 0.25f;
    [SerializeField] private GameObject rotationIndicatorPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private float minimumLightIntensity = 0.5f;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform parentForPlacedGates;
    [SerializeField] private LayerMask gateLayer;
    [SerializeField] private TMPro.TMP_Text currentGateTypeDisplayText;
    [SerializeField] private GameObject MainMenu;
    // private LineRenderer debugLineRenderer;

    private readonly float[] diagonals = new float[] { 0f, 60f, 120f };
    /// <summary>
    /// from 0 to 5, where 0 is 0 times rotated by 60 deg and 5 rotated by 300f
    /// </summary>
    private int lastTimesRotated = 0;
    private Transform currentRotationIndicator = null;
    private bool isInPlacementMode = false;
    private bool isInRotationMode = false;
    private readonly float diagonalLength = Mathf.Sqrt(3f); // length of diagonal in hex grid

    public bool HasPlacedGate { get; private set; } = false;

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

    public void DestroyAllPlacedGates()
    {
        foreach (Transform child in parentForPlacedGates)
        {
            Destroy(child.gameObject);
        }
        HasPlacedGate = false;
    }

    void Start()
    {
        OpenMainMenu();
        InputManager.instance.ItemRightAction.performed += ctx => OnGateTypeRight();
        InputManager.instance.ItemLeftAction.performed += ctx => OnGateTypeLeft();
    }

    void OpenMainMenu()
    {
        InputManager.instance.CancelAction.performed -= ctx => OpenMainMenu();
        mainMenuCtxAttached = false;
        MainMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    void CloseMainMenu()
    {
        MainMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    void OnGateTypeRight()
    {
        do
        {
            currentGateType = (GateType)(((int)currentGateType + 1) % gatePrefabs.Count);
        }
        while (currentGateType == GateType.DivergingLens || currentGateType == GateType.OneWayMirror);

        if (currentGateTypeDisplayText != null)
        {
            currentGateTypeDisplayText.text = GetCurrentGateTypeString();
        }
    }

    void OnGateTypeLeft()
    {
        do
        {
            currentGateType = (GateType)(((int)currentGateType - 1 + gatePrefabs.Count) % gatePrefabs.Count);
        }
        while (currentGateType == GateType.DivergingLens || currentGateType == GateType.OneWayMirror);

        if (currentGateTypeDisplayText != null)
        {
            currentGateTypeDisplayText.text = GetCurrentGateTypeString();
        }
    }

    private string GetCurrentGateTypeString()
    {
        return currentGateType switch
        {
            GateType.ConvergingLens => "Converging Lens",
            GateType.Mirror => "Mirror",
            GateType.OneWayMirror => "One-Way Mirror",
            GateType.Diffraction => "Diffraction Slate",
            _ => "Unknown Gate Type",
        };
    }

    void OnDestroy()
    {
        InputManager.instance.ItemRightAction.performed -= ctx => OnGateTypeRight();
        InputManager.instance.ItemLeftAction.performed -= ctx => OnGateTypeLeft();
    }

    private bool mainMenuCtxAttached = false;

    void AttachMainMenuContextIfNeeded()
    {
        if (!mainMenuCtxAttached)
        {
            InputManager.instance.CancelAction.performed += ctx => OpenMainMenu();
            mainMenuCtxAttached = true;
        }
    }

    void Update()
    {
        if (MainMenu.activeSelf && InputManager.instance.DashInput)
        {
            CloseMainMenu();
        }

        if (!isInPlacementMode && !isInRotationMode)
        {
            AttachMainMenuContextIfNeeded();
        }
        else if (mainMenuCtxAttached)
        {
            InputManager.instance.CancelAction.performed -= ctx => OpenMainMenu();
            mainMenuCtxAttached = false;
        }
            

        if (isInRotationMode)
        {
            RotationModeUpdate();
            return;
        }

        int gateCost = GetGateCost(currentGateType);

        if (gateCost > playerStats.CurrentSoul)
        {
            if (isInPlacementMode)
            {
                Time.timeScale = 1f;
                isInPlacementMode = false;
            }
            currentGateTypeDisplayText.alpha = 0.06f;
            currentGateTypeDisplayText.text = GetCurrentGateTypeString();
            DestroyAllIndicators();
            return;
        }
        else
        {
            currentGateTypeDisplayText.alpha = 0.5f;
            currentGateTypeDisplayText.text = "Press Ctrl to place: " + GetCurrentGateTypeString();
        }

        if (!InputManager.instance.PreciseControlInput)
        {
            if (isInPlacementMode)
            {
                Time.timeScale = 1f;
                isInPlacementMode = false;
            }
            DestroyAllIndicators();
            return;
        }

        if (!isInPlacementMode)
        {
            Time.timeScale = 0f;
            isInPlacementMode = true;
        }
        // Show places where you can build
        DestroyAllIndicators(); // children
        Vector2Int playerCell = (Vector2Int)hexGrid.WorldToCell(player.position);
        SearchForAllCellsToIndicate(playerCell, 5);
    }

    private int GetGateCost(GateType gateType)
    {
        return gateType switch
        {
            GateType.ConvergingLens => SoulEconomyManager.instance.ConvergingLensCost,
            GateType.Mirror => SoulEconomyManager.instance.MirrorCost,
            GateType.OneWayMirror => SoulEconomyManager.instance.OneWayMirrorCost,
            GateType.Diffraction => SoulEconomyManager.instance.DiffractionSlateCost,
            _ => int.MaxValue,
        };
    }

    private void DestroyAllIndicators()
    {
        foreach (Transform child in hexGrid.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void EnterGateRotationMode(Vector2 cellWorldPos)
    {
        isInPlacementMode = false;
        isInRotationMode = true;
        InputManager.instance.CancelAction.performed += ctx => CancelRotationMode();
        Time.timeScale = slowmoDuringRotation;
        DestroyAllIndicators();
        currentRotationIndicator = Instantiate(rotationIndicatorPrefab).transform;
        currentRotationIndicator.position = cellWorldPos;
    }

    private void PlaceGate()
    {
        if (currentRotationIndicator == null)
            return;

        playerStats.TakeSoul(GetGateCost(currentGateType));

        GameObject gatePrefab = gatePrefabs[(int)currentGateType];
        GameObject placedGate = Instantiate(gatePrefab, currentRotationIndicator.position, currentRotationIndicator.rotation, parentForPlacedGates);
        
        // Set layer for AI navigation
        if (gateLayer != 0)
        {
            SetLayerRecursively(placedGate, LayerMaskToLayer(gateLayer));
        }

        HasPlacedGate = true;
        
        CancelRotationMode();
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj.layer == layer) return; // Avoid redundant operations
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    private int LayerMaskToLayer(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 1)
        {
            layer >>= 1;
            layerNumber++;
        }
        return layerNumber;
    }

    private void RotationModeUpdate()
    {
        if (currentRotationIndicator == null)
            CancelRotationMode();

        if (InputManager.instance.DashInput)
        {
            PlaceGate();
            return;
        }

        if (InputManager.instance.IsKeyboardAndMouse)
        {
            HandleRotationWithMouse();
        }
        else if (InputManager.instance.RightStick != Vector2.zero)
        {
            RotateToFaceDirection(InputManager.instance.RightStick);
        }
        else
        {
            BackupRotation(currentRotationIndicator.position);
        }
    }

    private void HandleRotationWithMouse()
    {
        Vector2 direction = InputManager.instance.MousePosition - (Vector2)currentRotationIndicator.position;
        RotateToFaceDirection(direction);
    }

    private void RotateToFaceDirection(Vector2 direction)
    {
        float angle = Vector2.SignedAngle(Vector2.right, direction.normalized);
        
        // Normalize angle to 0-360 range
        if (angle < 0)
            angle += 360f;
        
        // The 6 valid directions for hex diagonals: 0°, 60°, 120°, 180°, 240°, 300°
        float[] validAngles = new float[] { 0f, 60f, 120f, 180f, 240f, 300f };
        
        // Find closest valid angle
        float closestAngle = validAngles[0];
        float minDifference = float.MaxValue;
        int closestIndex = 0;
        
        for (int i = 0; i < validAngles.Length; i++)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, validAngles[i]));
            if (diff < minDifference)
            {
                minDifference = diff;
                closestAngle = validAngles[i];
                closestIndex = i;
            }
        }
        
        // Update rotation indicator
        currentRotationIndicator.rotation = Quaternion.Euler(0, 0, closestAngle);
        lastTimesRotated = closestIndex;
    }

    private void BackupRotation(Vector2 cellWorldPos)
    {
        for (int i = 0; i < 6; i++)
        {
            if (IsPossibleToPlaceGateOnDiagonal((Vector2Int)hexGrid.WorldToCell(cellWorldPos), lastTimesRotated < diagonals.Length ? lastTimesRotated : lastTimesRotated/2))
            {
                for (int _ = 0; _ < lastTimesRotated; ++_)
                {
                    currentRotationIndicator.Rotate(new Vector3(0,0,60f));
                }
                return;
            }
        }

        CancelRotationMode();
        return;
    }

    private void CancelRotationMode()
    {
        isInRotationMode = false;
        isInPlacementMode = true;
        InputManager.instance.CancelAction.performed -= ctx => CancelRotationMode();
        Time.timeScale = 1f;
        if (currentRotationIndicator != null)
        {
            Destroy(currentRotationIndicator.gameObject);
            currentRotationIndicator = null;
        }
    }

    private void SearchForAllCellsToIndicate(Vector2Int playerCell, int radius)
    {
        // if (debugLineRenderer != null)
        // {
        //     debugLineRenderer.positionCount = 0;
        // }
        Vector2 playerCellWorldPosition = (Vector2)hexGrid.GetCellCenterWorld((Vector3Int)playerCell);
        // Start from 1 to radius going through all hexagonal cells
        for (int r = 0; r < radius; r++)
        {
            float stepAngle = 360f / (6 * (r+1));
            for (float angle = 90f; angle < 450f; angle += stepAngle)
            {
                float x = hexGrid.cellSize.x * (r + 1) * Mathf.Sin(angle * Mathf.Deg2Rad);
                float y = hexGrid.cellSize.x * (r + 1) * Mathf.Cos(angle * Mathf.Deg2Rad);

                Vector2 cellWorldPosition = playerCellWorldPosition + new Vector2(x, y);
                Vector2Int cellPosition = (Vector2Int)hexGrid.WorldToCell((Vector3)cellWorldPosition);

                if (OutOfScreen(cellPosition))
                {
                    Debug.Log("Out of screen: " + cellPosition);
                    continue;
                }

                if (IsPossibleToPlaceGateInCell(cellPosition))
                {
                    // if (debugLineRenderer != null)
                    // {
                    //     debugLineRenderer.positionCount++;
                    //     debugLineRenderer.SetPosition(debugLineRenderer.positionCount - 1, hexGrid.GetCellCenterWorld((Vector3Int)cellPosition));
                    // }
                    InstantiateIndicatorAtCell(cellPosition);
                }
            }
        }
    }

    private bool OutOfScreen(Vector2Int cellPosition)
    {
        Vector3 cellWorldPosition = hexGrid.GetCellCenterWorld((Vector3Int)cellPosition);
        if (cam == null)
            cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = cam.aspect * screenHeight;

        return cellWorldPosition.x < cam.transform.position.x - screenWidth / 2f ||
               cellWorldPosition.x > cam.transform.position.x + screenWidth / 2f ||
               cellWorldPosition.y < cam.transform.position.y - screenHeight / 2f ||
               cellWorldPosition.y > cam.transform.position.y + screenHeight / 2f;
    }

    private void InstantiateIndicatorAtCell(Vector2Int cellPosition)
    {
        Vector3 cellWorldPosition = hexGrid.GetCellCenterWorld((Vector3Int)cellPosition);
        Instantiate(placementIndicatorPrefab, cellWorldPosition, Quaternion.identity, hexGrid.transform);
    }

    private bool IsPossibleToPlaceGateInCell(Vector2Int cellPosition)
    {
        // Check light intensity in the center
        Vector3 cellWorldPosition = hexGrid.GetCellCenterWorld((Vector3Int)cellPosition);
        float lightIntensity = GetLightIntensityAtPosition(cellWorldPosition);
        
        if (lightIntensity < minimumLightIntensity)
        {
            //Debug.Log("Insufficient light at " + cellPosition + ": " + lightIntensity);
            return false;
        }

        for (int i = 0; i < diagonals.Length; i++)
        {
            if (IsPossibleToPlaceGateOnDiagonal(cellPosition, i))
            {
                return true;
            }
        }

        Debug.Log("Cannot place gate on any diagonal at " + cellPosition);
        return false;
    }

    private bool IsPossibleToPlaceGateOnDiagonal(Vector2Int cellPosition, int diagonalIndex)
    {
        Vector2 cellWorldPosition = hexGrid.GetCellCenterWorld((Vector3Int)cellPosition);
        Vector2 diagonalStart = cellWorldPosition + (diagonalIndex == 0 ? new(0, -hexGrid.cellSize.y/2) : new (-hexGrid.cellSize.x/2, -(diagonals[diagonalIndex]/30 + 3)*diagonalLength/2));
        Vector2 diagonalEnd = cellWorldPosition + (diagonalIndex == 0 ? new(0, hexGrid.cellSize.y/2) : new (hexGrid.cellSize.x/2, (diagonals[diagonalIndex]/30 + 3)*diagonalLength/2));
        RaycastHit2D[] hits = Physics2D.LinecastAll(diagonalStart, diagonalEnd);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && CompareTags(hit.collider.gameObject))
            {
                return false;
            }
        }

        return true;

        static bool CompareTags(GameObject obj)
        {
            return obj.CompareTag("Player") || obj.CompareTag("Gate") || obj.CompareTag("Enemy");
        }
    }

    private float GetLightIntensityAtPosition(Vector3 position)
    {
        // Only check lights that could possibly reach this position
        // Using a conservative search radius based on typical max light radius
        const float maxExpectedLightRadius = 10f; // Adjust based on your game
        
        UnityEngine.Rendering.Universal.Light2D[] allLights = FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None);
        
        float totalIntensity = 0f;
        
        foreach (var light in allLights)
        {
            if (!light.enabled)
                continue;

            float distance = Vector2.Distance(position, light.transform.position);
            
            // Early rejection - skip lights too far away to possibly affect this position
            if (distance > maxExpectedLightRadius)
                continue;
            
            // Check if position is within light's actual outer radius
            if (distance <= light.pointLightOuterRadius)
            {
                // Simple attenuation based on distance
                float attenuation = 1f - (distance / light.pointLightOuterRadius);
                totalIntensity += light.intensity * attenuation;
            }
        }
        
        return totalIntensity;
    }
}
