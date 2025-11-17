using UnityEngine;

public class EnergyMeleeAI : MonoBehaviour
{
    [SerializeField] private Enemy enemyData;
    private Transform player;

    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
    }

    void Update()
    {
        GoInPlayerDirection();
    }

    private void GoInPlayerDirection()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += enemyData.speed * Time.deltaTime * direction;
    }
}
