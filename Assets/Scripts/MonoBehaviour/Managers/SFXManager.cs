using System;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;

    [Serializable]
    public class SFX
    {
        public string Name;
        public AudioClip[] Clips;
        public Vector2 Volume;
        public Vector2 Pitch;
    }
    
    [SerializeField] private GameObject sfxAudioSourcePrefab;
    [SerializeField] private int poolSize = 5;

    [SerializeField] private List<SFX> enemyAttacks;

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

    private void OnValidate()
    {
        if (enemyAttacks != null)
        {
            foreach (var sfx in enemyAttacks)
            {
                if (sfx != null)
                {
                    if (sfx.Clips == null || sfx.Clips.Length == 0)
                    {
                        sfx.Clips = new AudioClip[1];
                    }
                    if (sfx.Volume == Vector2.zero)
                        sfx.Volume = new Vector2(1f, 1f);
                    if (sfx.Pitch == Vector2.zero)
                        sfx.Pitch = new Vector2(1f, 1f);
                }
            }
        }
    }

    public void PlaySFX(AudioClip clip, Vector2 position, float volume = 1f, float pitch = 1f)
    {
        if (HasIssues()) return;

        GameObject obj = ObjectPooler.instance.GetFromPool(sfxAudioSourcePrefab, position, null, poolSize);
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
        StartCoroutine(ObjectPooler.instance.ReturnToPool(sfxAudioSourcePrefab, obj, clip.length, true));
    
        bool HasIssues()
        {
            if (clip == null)
            {
                Debug.LogWarning("SFXManager: Attempted to play a null AudioClip.");
                return true;
            }
            if (position == null)
            {
                Debug.LogWarning("SFXManager: Position is null. Defaulting to Vector2.zero.");
                position = Vector2.zero;
            }
            if (volume < 0f || volume > 1f)
            {
                Debug.LogWarning("SFXManager: Volume out of range (0 to 1). Given: " + volume);
                volume = Mathf.Clamp01(volume);
            }
            if (pitch < -3f || pitch > 3f)
            {
                Debug.LogWarning("SFXManager: Pitch out of range (-3 to 3). Given: " + pitch);
                pitch = Mathf.Clamp(pitch, -3f, 3f);
            }
            return false;
        }
    }

    #region Overloads
    
    public void PlaySFX(AudioClip[] clips, Vector2 position, Vector2 volumeRange, Vector2 pitchRange)
    {
        if (volumeRange.x > volumeRange.y)
        {
            Debug.LogWarning("Volume range min is greater than max. Swapping values.");
            (volumeRange.x, volumeRange.y) = (volumeRange.y, volumeRange.x);
        }
        if (pitchRange.x > pitchRange.y)
        {
            Debug.LogWarning("Pitch range min is greater than max. Swapping values.");
            (pitchRange.x, pitchRange.y) = (pitchRange.y, pitchRange.x);
        }

        int randomIndex = UnityEngine.Random.Range(0, clips.Length);
        int randomVolume = UnityEngine.Random.Range(Mathf.RoundToInt(volumeRange.x * 100), Mathf.RoundToInt(volumeRange.y * 100));
        int randomPitch = UnityEngine.Random.Range(Mathf.RoundToInt(pitchRange.x * 100), Mathf.RoundToInt(pitchRange.y * 100));

        PlaySFX(clips[randomIndex], position, randomVolume / 100f, randomPitch / 100f);
    }

    #endregion
}
