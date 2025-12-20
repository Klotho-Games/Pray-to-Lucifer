using UnityEngine;

[CreateAssetMenu(fileName = "DestroyGate", menuName = "Click Actions/In-Game/Destroy Gate")]
public class DestroyGateAction : ClickActionSO
{
    [SerializeField] private float detectionRadius = 0.1f;
    [SerializeField] private string gateLayerName = "Gate";
    [SerializeField] private string gateTag = "Gate";
    
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.DestroyGateSFX, source.transform.position);
        
        Vector2 position = source.transform.position;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, detectionRadius, LayerMask.GetMask(gateLayerName));
        
        foreach (var col in colliders)
        {
            if (col.CompareTag(gateTag))
            {
                GatePlacementManager.instance.HasDestroyedGate = true;
                Destroy(col.gameObject);
                return;
            }
        }

        GatePlacementManager.instance.TryShowPlacementIndicator(cellWorldPosition: source.transform.position);

        // hide the destruction indicator
        ObjectPooler.instance.ReturnToPool(source.gameObject, source.gameObject);
    }
}
