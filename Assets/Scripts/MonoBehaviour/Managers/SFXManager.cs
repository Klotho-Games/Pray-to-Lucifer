using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;

    [SerializeField] private GameObject sfxAudioSourcePrefab;
    [SerializeField] private int poolSize = 5;

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

    public void PlaySFX(AudioClip clip, Vector2 position, float volume = 1f)
    {
        GameObject obj = ObjectPooler.instance.GetFromPool(sfxAudioSourcePrefab, position, null, poolSize);
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        StartCoroutine(ObjectPooler.instance.ReturnToPool(sfxAudioSourcePrefab, obj, clip.length, true));
    }

    public void PlaySFX(AudioClip[] clips, Vector2 position, float volume = 1f)
    {
        if (clips.Length == 0) return;
        int randomIndex = Random.Range(0, clips.Length);
        PlaySFX(clips[randomIndex], position, volume);
    }
}
