using System.Collections;
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
    public static ObjectPooler instance;

    [Header("Enemy Pooling")]
    [SerializeField] private const int defaultPoolSize = 100;
    private Dictionary<GameObject, Queue<GameObject>> pools = new();
    private List<GameObject> inactiveObjects = new();

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
            obj.transform.parent = transform;
            var pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.prefabReference = prefab;
            queue.Enqueue(obj);
        }
        pools[prefab] = queue;
    }

    // Weighted prefab selection is now handled by the manager using scriptable objects.

    public GameObject GetFromPool(GameObject prefab, Vector3 position, Transform parent, int poolSize = defaultPoolSize)
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
        obj.transform.SetPositionAndRotation(position, Quaternion.identity);
        obj.transform.parent = parent;
        obj.SetActive(true);
        return obj;
    }

    // Removed obsolete GetFromPoolWeightedByTimeline method.

    public void ReturnToPool(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.parent = transform;
        var pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj != null && pooledObj.prefabReference != null && pools.ContainsKey(pooledObj.prefabReference))
        {
            pools[pooledObj.prefabReference].Enqueue(obj);
        };
    }

    public IEnumerator ReturnToPool(GameObject prefab, GameObject obj, float delay, bool isRealtime)
    {
        yield return isRealtime ? new WaitForSecondsRealtime(delay) : new WaitForSeconds(delay);
        ReturnToPool(prefab, obj);
    }

    public void ActivateEnemiesForGroup(GameObject prefab, int quantity, Vector3[] positions)
    {
        int activated = 0;
        for (int i = 0; i < inactiveObjects.Count && activated < quantity; i++)
        {
            if (!inactiveObjects[i].activeSelf && inactiveObjects[i].GetComponent<PooledObject>().prefabReference == prefab)
            {
                inactiveObjects[i].transform.position = positions[activated];
                inactiveObjects[i].SetActive(true);
                activated++;
            }
        }
    }
}
