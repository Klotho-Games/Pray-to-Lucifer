using System.Collections.Generic;
using UnityEngine;

public class EnergyAI : MonoBehaviour
{
    [SerializeField] private EnemySO enemyData;
    [SerializeField] private Collider2D coll;
    private Transform PlayerTransform;

    public void Initialize(Transform playerTransform)
    {
        PlayerTransform = playerTransform;
    }

    void Update()
    {
        if (gameObject.activeInHierarchy == false)
            return;
        
        if (PlayerDoesNotOverlapWithEnemyCollider())
        {
            GoInPlayerDirection();
        }
    }

    private bool PlayerDoesNotOverlapWithEnemyCollider()
    {
        List<Collider2D> results = new();
        ContactFilter2D filter = new();
        filter.SetLayerMask(LayerMask.GetMask("Player"));
        filter.useLayerMask = true;
        
        Physics2D.OverlapCollider(coll, filter, results);

        return results.Count == 0;
    }

    private void GoInPlayerDirection()
    {
        if (PlayerTransform == null) return;

        Vector3 direction = (PlayerTransform.position - transform.position).normalized;
        transform.position += enemyData.Speed * Time.deltaTime * direction;
    }
}
