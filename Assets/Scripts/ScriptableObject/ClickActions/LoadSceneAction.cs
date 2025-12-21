using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "LoadScene", menuName = "Click Actions/Load Scene")]
public class LoadSceneAction : ClickActionSO
{
    [SerializeField] private string sceneName;
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;
    
    public override void Execute(Clickable2D source)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"Scene name is not set on {name}");
            return;
        }
            
        SceneManager.LoadScene(sceneName, loadMode);
    }
    
    public override bool CanExecute(Clickable2D source)
    {
        return !string.IsNullOrEmpty(sceneName);
    }
}
