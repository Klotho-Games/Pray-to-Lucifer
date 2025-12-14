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

    public void StopTime()
    {
        Time.timeScale = 0f;

        BeamController.instance.IsBeamActive = false;
        BeamController.instance.DeactivateBeam();
    }

    public void ResumeTime()
    {
        Time.timeScale = 1f;
    }
}
