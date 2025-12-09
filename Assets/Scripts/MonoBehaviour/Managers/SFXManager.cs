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

    [Header("Enemy SFX")]
    [SerializeField] private List<SFX> enemyAttacks;
    [SerializeField] private List<SFX> enemyDeaths;

    [Header("Beam SFX")]
    public SFX BeamStartSFX;
    public SFX BeamLoopSFX;
    public SFX BeamEndSFX;

    [Header("Player SFX")]
    public SFX HealSoulStateSFX;
    public SFX RespawnSFX;

    [Header("Densoul SFX")]
    public SFX ChangeGateTypeSFX;
    public SFX EnterGatePlacementModeSFX;
    public SFX GatePlacementModeLoopSFX;
    public SFX LeaveGatePlacementModeSFX;
    public SFX EnterRotationModeSFX;
    public SFX RotateGateSFX;
    public SFX PlaceGateSFX;
    public SFX DestroyGateSFX;

    [Header("Soul Shard SFX")]
    public SFX CollectSoulShardSFX;
    public SFX MergeSoulShardSFX;

    [Header("Main Menu SFX")]
    public SFX TutorialButtonSFX;
    public SFX PlayButtonSFX;
    public SFX ExitButtonSFX;


    private List<GameObject> activeLoopingSFX = new(); 

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
        if (enemyDeaths != null)
        {
            foreach (var sfx in enemyDeaths)
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

    public void PlaySFX(AudioClip clip, Vector2 position, float volume = 1f, float pitch = 1f, string sfxName = "")
    {
        if (HasIssues()) return;

        GameObject obj = ObjectPooler.instance.GetFromPool(sfxAudioSourcePrefab, position, null, poolSize);
        if (sfxName != "")
            obj.name = sfxName + " SFX";
        else
            obj.name = clip.name + " SFX";
        AudioSource audioSource = obj.GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.loop = false;
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

    public void PlaySFX(SFX sfx, Vector2 position)
    {
        if (sfx == null || sfx.Clips == null || sfx.Clips.Length == 0)
        {
            Debug.LogWarning("SFXManager: Attempted to play a null SFX.");
            return;
        }
        PlaySFX(sfx.Clips, position, sfx.Volume, sfx.Pitch);
    }

    #endregion

    public void PlayEnemyAttackSFX(string enemyName, Vector2 position)
    {
        SFX sfx = enemyAttacks.Find(s => s.Name == enemyName);
        if (sfx != null)
        {
            PlaySFX(sfx.Clips, position, sfx.Volume, sfx.Pitch);
        }
        else
        {
            Debug.LogWarning("SFXManager: No Enemy Attack SFX found with the name " + enemyName);
        }
    }

    public void PlayEnemyDeathSFX(string enemyName, Vector2 position)
    {
        SFX sfx = enemyDeaths.Find(s => s.Name == enemyName);
        if (sfx != null)
        {
            PlaySFX(sfx.Clips, position, sfx.Volume, sfx.Pitch);
        }
        else
        {
            Debug.LogWarning("SFXManager: No Enemy Death SFX found with the name " + enemyName);
        }
    }

    #region Looping SFX

    public void StartLoopingSFX(SFX sfx, Vector2 position, string sfxName = "")
    {
        if (sfx == null || sfx.Clips == null || sfx.Clips.Length == 0)
        {
            Debug.LogWarning("SFXManager: Attempted to start a null looping SFX.");
            return;
        }

        if(sfxName == "")
        {
            sfxName = sfx.Name;
        }
        if (LoopSFXExists(sfxName, out AudioSource source))
        {
            Debug.Log("Updating looping SFX: " + sfxName);
            // SFX is already playing, just update position and parameters
            source.clip = sfx.Clips[UnityEngine.Random.Range(0, sfx.Clips.Length)];
            source.transform.position = position;
            source.volume = UnityEngine.Random.Range(sfx.Volume.x, sfx.Volume.y);
            source.pitch = UnityEngine.Random.Range(sfx.Pitch.x, sfx.Pitch.y);
            if (!source.isPlaying)
            {
                source.Play();
            }
        }
        else
        {
            Debug.Log("Starting looping SFX: " + sfxName);
            source = ObjectPooler.instance.GetFromPool(sfxAudioSourcePrefab, position, null, poolSize).GetComponent<AudioSource>();
            source.loop = true;
            source.clip = sfx.Clips[UnityEngine.Random.Range(0, sfx.Clips.Length)];
            source.transform.position = position;
            source.volume = UnityEngine.Random.Range(sfx.Volume.x, sfx.Volume.y);
            source.pitch = UnityEngine.Random.Range(sfx.Pitch.x, sfx.Pitch.y);
            source.Play();

            source.gameObject.name = sfxName;
            activeLoopingSFX.Add(source.gameObject);
        }
    }

    public void StopLoopingSFX(string name)
    {
        if (LoopSFXExists(name, out AudioSource source))
        {
            Debug.Log("Stopping looping SFX: " + name);
            if (source != null && source.isPlaying)
            {
                source.Stop();
                source.loop = false;
            }
            ObjectPooler.instance.ReturnToPool(source.gameObject, source.gameObject);
            activeLoopingSFX.Remove(source.gameObject);
        }
        else
        {
            Debug.LogWarning("SFXManager: No looping SFX found with the name " + name);
        }
    }

    private bool LoopSFXExists(string name, out AudioSource source)
    {
        source = null;
        GameObject sfxObject = activeLoopingSFX.Find(sfx => sfx.name == name);
        return sfxObject != null && sfxObject.TryGetComponent(out source);
    }

    #endregion
}
