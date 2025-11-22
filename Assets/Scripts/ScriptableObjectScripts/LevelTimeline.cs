using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [ShowIf("scattered", false)] public float groupRadius = 5f;
        public int spawnWeight = 100;
    }

    public Vector2 spawnPosition = Vector2.zero;
    public List<EnemyGroup> enemyGroups;

#if UNITY_EDITOR
    private void OnDisable()
    {
        if (enemyGroups != null && enemyGroups.Count > 0)
        {
            enemyGroups.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
