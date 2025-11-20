using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTimeline", menuName = "Scriptable Objects/LevelTimeline")]
public class LevelTimeline : ScriptableObject
{
    [System.Serializable]
    public class EnemyGroup
    {
        public float spawnTime;
        public GameObject enemyPrefab;
        public int quantity;
        public bool scattered;
        [ShowIf("scattered", false)]
        public float groupRadius = 5f;
    }

    public Vector2 spawnPosition = Vector2.zero;
    public List<EnemyGroup> enemyGroups;

    private void OnValidate()
    {
        SortEnemyGroupsBySpawnTime();
        void SortEnemyGroupsBySpawnTime()
        {
            enemyGroups.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        }
    }
}
