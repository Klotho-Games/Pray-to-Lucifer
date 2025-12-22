using UnityEngine;

public class CheckLevelMeantToBeLoaded : MonoBehaviour
{
    void Start()
    {
        GetComponent<TMPro.TextMeshPro>().text = PlayerPrefs.GetInt(LevelManager.LevelToLoadIndexKey, -1).ToString();
        Debug.Log("Level meant to be loaded index: " + PlayerPrefs.GetInt(LevelManager.LevelToLoadIndexKey, -1));
    }
}
