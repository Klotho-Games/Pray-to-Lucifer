using UnityEngine;

public class GatePlacement : MonoBehaviour
{
    public static GatePlacement instance;

    [SerializeField] private Grid hexGrid;
    [SerializeField] private GameObject placementIndicatorPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerSoulState soulState;
    [SerializeField] private float minimumLightIntensity = 0.5f;

    private float[] diagonals = new float[] { 0f, 60f, 120f };
    private readonly float diagonalLength = Mathf.Sqrt(3f); // length of diagonal in hex grid

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
        Debug.LogWarning("TODO get facing direction from animator or player controller as a fallback for beam direction");
    }
    #endregion

    void Update()
    {
        if (soulState.currentSoulState is not null)
            return;

        DestroyAllIndicators(); // children
        Vector2Int playerCell = PointToGridCell(player.position);
        SearchForAllCellsToIndicate(playerCell, 3);
    }

    private void DestroyAllIndicators()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void EnterGatePlacementMode()
    {
        Time.timeScale = 0f;
        Vector2Int playerCell = PointToGridCell(player.position);
        SearchForAllCellsToIndicate(playerCell, 3);
    }

    private void SearchForAllCellsToIndicate(Vector2Int playerCell, int radius)
    {
        // Start from player cell and search in a radius
        for (int q = -radius; q <= radius; q++)
        {
            for (int r = -radius; r <= radius; r++)
            {
                // Skip cells outside hexagonal radius
                if (Mathf.Abs(q + r) > radius)
                    continue;

                Vector2Int cellPosition = new(playerCell.x + q, playerCell.y + r);

                if (OutOfScreen(cellPosition))
                    continue;

                if (IsPossibleToPlaceGateInCell(cellPosition))
                {
                    InstantiateIndicatorAtCell(cellPosition);
                }
            }
        }
    }

    private bool OutOfScreen(Vector2Int cellPosition)
    {
        Vector3 cellWorldPosition = hexGrid.GetCellCenterWorld((Vector3Int)cellPosition);
        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(cellWorldPosition);

        return viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f;
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
            return false;
        }

        for (int i = 0; i < diagonals.Length; i++)
        {
            if (!IsPossibleToPlaceGateOnDiagonal(cellPosition, i))
            {
                return false;
            }
        }

        return true;
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


    private Vector2Int PointToGridCell(Vector2 point)
    {
        Vector3Int cellPosition = hexGrid.WorldToCell(point);
        return (Vector2Int)cellPosition;
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
