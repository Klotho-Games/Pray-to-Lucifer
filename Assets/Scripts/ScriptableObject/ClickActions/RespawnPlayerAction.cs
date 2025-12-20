using UnityEngine;

[CreateAssetMenu(fileName = "RespawnPlayer", menuName = "Click Actions/In-Game/Respawn Player")]
public class RespawnPlayerAction : ClickActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.RespawnSFX, source.transform.position);
        LevelManager.instance.RespawnPlayer();
    }
}
