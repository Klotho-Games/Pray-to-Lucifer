using UnityEngine;

[CreateAssetMenu(fileName = "RespawnPlayer", menuName = "Actions/In-Game/Respawn Player")]
public class RespawnPlayerAction : ActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.RespawnSFX, source.transform.position);
        LevelManager.instance.RespawnPlayer();
    }
}
