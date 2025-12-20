using UnityEngine;

[CreateAssetMenu(fileName = "PlaceGate", menuName = "Click Actions/In-Game/Place Gate")]
public class PlaceGateAction : ClickActionSO
{
    [SerializeField] private bool deactivateSourceAfterPlacement = true;
    
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.PlaceGateSFX, source.transform.position);
        
        if (deactivateSourceAfterPlacement)
        {
            source.gameObject.SetActive(false);
        }
        
        GatePlacementManager.instance.PlaceGate();

        source.gameObject.SetActive(false);
    }
}
