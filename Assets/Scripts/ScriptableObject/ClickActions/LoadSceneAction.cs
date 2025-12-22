using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "LoadScene", menuName = "Click Actions/Load Scene")]
public class LoadSceneAction : ClickActionSO
{
    [SerializeField] private SceneField sceneToLoad;
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;
    
    public override void Execute(Clickable2D source)
    {            
        SceneManager.LoadScene(sceneToLoad, loadMode);
    }
    
    public override bool CanExecute(Clickable2D source)
    {
        if (sceneToLoad == null || string.IsNullOrEmpty(sceneToLoad.SceneName))
        {
            Debug.LogError($"Scene name is not set on {name}");
            return false;
        }
        return true;
    }
}
