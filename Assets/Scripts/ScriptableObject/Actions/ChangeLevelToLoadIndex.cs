using UnityEngine;

[CreateAssetMenu(fileName = "Change Level To Load Index", menuName = "Actions/Change Level To Load Index")]
public class ChangeLevelToLoadIndex : ActionSO
{
    [SerializeField] private int levelToLoadIndex;
    
    public override void Execute(Clickable2D source)
    {            
        PlayerPrefs.SetInt(LevelManager.LevelToLoadIndexKey, levelToLoadIndex);
    }
}
