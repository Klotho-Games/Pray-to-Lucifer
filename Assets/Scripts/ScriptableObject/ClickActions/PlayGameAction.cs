using UnityEngine;

[CreateAssetMenu(fileName = "PlayGame", menuName = "Click Actions/Main Menu/Play Game")]
public class PlayGameAction : ClickActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.PlayButtonSFX, source.transform.position);
        LevelManager.instance.StartGame();
    }
}
