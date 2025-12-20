using UnityEngine;

[CreateAssetMenu(fileName = "CloseGame", menuName = "Click Actions/Main Menu/Close Game")]
public class CloseGameAction : ClickActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.ExitButtonSFX, source.transform.position);
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
