using UnityEngine;
using PrimeTween;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Transform))]
public class SoulShard : MonoBehaviour
{
    public bool isMerging = false;
    public int soulAmount = 0;
    [SerializeField] private Sprite[] soulShardSpriteVariants;
    private SpriteRenderer spriteRenderer;

    public void Initialize(int amount)
    {
        soulAmount = amount;
        CheckToMerge();
        GetComponent<Transform>().localScale = Vector3.one * Mathf.Log10(soulAmount / 10f);
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = soulShardSpriteVariants[Random.Range(0, soulShardSpriteVariants.Length)];
    }

    private void CheckToMerge()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
        foreach (var col in colliders)
        {
            if (col.gameObject != gameObject && col.TryGetComponent(out SoulShard otherShard) && otherShard.isMerging == false)
            {
                soulAmount += otherShard.soulAmount;
                otherShard.soulAmount = 0;
                otherShard.Merge(transform);
            }
        }
    }

    public void Merge(Transform target, float duration = 0.5f)
    {
        isMerging = true;
        GetComponent<Collider2D>().enabled = false; // Prevent re-trigger
        Tween.Position(transform, target.position, duration, ease: Ease.InCubic)
            .OnComplete(() => Destroy(gameObject));
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
