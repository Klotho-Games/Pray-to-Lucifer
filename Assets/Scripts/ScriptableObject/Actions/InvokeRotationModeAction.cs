using UnityEngine;

[CreateAssetMenu(fileName = "InvokeRotationMode", menuName = "Actions/In-Game/Rotation Mode")]
public class InvokeRotationModeAction : ActionSO
{
    public override void Execute(Clickable2D source)
    {
        Vector2 cellWorldPos = source.transform.position;
        GatePlacementManager.instance.EnterGateRotationMode(cellWorldPos);
    }
}
