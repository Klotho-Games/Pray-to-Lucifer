using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyGroup", menuName = "EnemyGroup")]
[System.Serializable]
public class EnemyGroup
{
    public GameObject enemyPrefab;
    public int quantity;
    public float spawnTime;
    public float groupRadius;
    public bool scattered;
    public int spawnWeight = 100;
}