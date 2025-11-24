using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoolEntry
{
    public GameObject prefab;
    [Range(0, 100)] public int spawnWeight = 100;
}

public class ObjectPooler : MonoBehaviour
{
    [Header("Enemy Pooling")]
    [SerializeField] private int poolSize = 100;
    private Dictionary<GameObject, Queue<GameObject>> pools = new();
    private List<GameObject> inactiveEnemies = new();

    void Awake()
    {
        // Pools should be created by manager using scriptable object configuration
    }

    // Removed obsolete PrewarmPoolsFromTimeline method.

    private GameObject CreatePoolInstance(GameObject prefab)
    {
        var obj = Instantiate(prefab);
        obj.SetActive(false);
        var pooledObj = obj.AddComponent<PooledObject>();
        pooledObj.prefabReference = prefab;
        return obj;
    }

    public void CreatePool(GameObject prefab, int size)
    {
        if (pools.ContainsKey(prefab)) return;
        var queue = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            var obj = Instantiate(prefab);
            obj.SetActive(false);
            var pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.prefabReference = prefab;
            queue.Enqueue(obj);
        }
        pools[prefab] = queue;
    }

    // Weighted prefab selection is now handled by the manager using scriptable objects.

    public GameObject GetFromPool(GameObject prefab, Vector3 position)
    {
        if (!pools.ContainsKey(prefab))
            CreatePool(prefab, poolSize);
        var queue = pools[prefab];
        GameObject obj = null;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab);
            var pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.prefabReference = prefab;
        }
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(true);
        return obj;
    }

    // Removed obsolete GetFromPoolWeightedByTimeline method.

    public void ReturnToPool(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        var pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj != null && pooledObj.prefabReference != null && pools.ContainsKey(pooledObj.prefabReference))
        {
            pools[pooledObj.prefabReference].Enqueue(obj);
        }
    }

    public void ActivateEnemiesForGroup(GameObject prefab, int quantity, Vector3[] positions)
    {
        int activated = 0;
        for (int i = 0; i < inactiveEnemies.Count && activated < quantity; i++)
        {
            if (!inactiveEnemies[i].activeSelf && inactiveEnemies[i].GetComponent<PooledObject>().prefabReference == prefab)
            {
                inactiveEnemies[i].transform.position = positions[activated];
                inactiveEnemies[i].SetActive(true);
                activated++;
            }
        }
    }
}
