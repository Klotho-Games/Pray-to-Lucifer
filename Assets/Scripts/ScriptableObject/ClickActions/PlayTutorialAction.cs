using UnityEngine;

[CreateAssetMenu(fileName = "PlayTutorial", menuName = "Click Actions/Main Menu/Play Tutorial")]
public class PlayTutorialAction : ClickActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.TutorialButtonSFX, source.transform.position);
        LevelManager.instance.StartTutorial();
    }
}
