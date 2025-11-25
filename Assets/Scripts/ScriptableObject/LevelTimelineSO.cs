using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTimeline", menuName = "Scriptable Objects/LevelTimeline")]
public class LevelTimelineSO : ScriptableObject
{
    [Serializable]
    public class WaveEntry
    {
        [Tooltip("Time in seconds since end of previous wave")]
        public float delayAfterPreviousWave;
        public WaveDataSO waveData;
    }

    public List<WaveEntry> subsequentWaves = new();
    public Vector2 playerSpawnPosition = Vector2.zero;
}
