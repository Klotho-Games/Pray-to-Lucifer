using UnityEngine;

[CreateAssetMenu(fileName = "PlayGame", menuName = "Actions/Main Menu/Play Game")]
public class PlayGameAction : ActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.PlayButtonSFX, source.transform.position);
        LevelManager.instance.StartGame();
    }
}
