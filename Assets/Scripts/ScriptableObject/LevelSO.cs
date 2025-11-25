using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Scriptable Objects/LevelTimeline")]
public class LevelSO : ScriptableObject
{
    [Serializable]
    public class Wave
    {
        [Tooltip("Time in seconds since end of previous wave")]
        public float delayAfterPreviousWave;
        public WaveDataSO waveData;
    }

    public Wave[] waves;
    public Vector2 playerSpawnPosition = Vector2.zero;
    public Sprite floorMap;
}
