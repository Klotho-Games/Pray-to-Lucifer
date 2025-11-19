using UnityEngine;

public class MaterialProjectileAI : MonoBehaviour
{
    private Transform playerTransform;

    public void Initialize(Transform player)
    {
        playerTransform = player;
    }
}
