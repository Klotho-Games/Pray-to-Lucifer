using UnityEngine;

public class ProjectManager : MonoBehaviour
{
    public static ProjectManager instance;
    
    #region Instance
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
