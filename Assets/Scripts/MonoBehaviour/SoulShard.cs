using UnityEngine;
using PrimeTween;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class SoulShard : MonoBehaviour
{
    public int soulAmount = 0;
    [SerializeField] private Sprite[] soulShardSpriteVariants;
    private SpriteRenderer spriteRenderer;

    public void Initialize(int amount)
    {
        soulAmount = amount;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = soulShardSpriteVariants[Random.Range(0, soulShardSpriteVariants.Length)];
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            playerStats.AddSoul(soulAmount);
            TweenToPlayerAndDestroy(other.transform);
        }
        
    }

    private void TweenToPlayerAndDestroy(Transform playerTransform)
    {
        GetComponent<Collider2D>().enabled = false; // Prevent re-trigger
        StartCoroutine(ChasePlayer(playerTransform));
    }

    private IEnumerator ChasePlayer(Transform target)
    {
        float speed = 15f;
        float acceleration = 30f;
        float currentSpeed = 0f;
        
        while (Vector3.Distance(transform.position, target.position) > 0.1f)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.deltaTime, speed);
            transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
