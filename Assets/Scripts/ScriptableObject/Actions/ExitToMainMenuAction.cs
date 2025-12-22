using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "ExitToMainMenu", menuName = "Actions/Exit to Main Menu")]
public class ExitToMainMenuAction : ActionSO
{
    public override void Execute(Clickable2D source)
    {
        SFXManager.instance.PlaySFX(SFXManager.instance.ExitButtonSFX, source.transform.position);
        SceneManager.LoadScene("MainMenu");
    }
}
