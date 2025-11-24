using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WavePrefabEntry
{
    public GameObject prefab;
    [Range(0, 100)] public int spawnWeight = 100;
}

[Serializable]
public class WaveData
{
    [Tooltip("Duration of this wave in seconds")]
    public float waveDuration = 30f;
    [Tooltip("Initial spawn interval in seconds")]
    public float startingSpawnInterval = 1.0f;
    [Tooltip("Amount to decrease spawn interval per second")]
    public float spawnIntervalDecrement = 0.01f;
    [Tooltip("Minimum allowed spawn interval")]
    public float minSpawnInterval = 0.1f;
    [Tooltip("Prefabs and their spawn weights for this wave")]
    public List<WavePrefabEntry> prefabsWithWeights = new();
}
