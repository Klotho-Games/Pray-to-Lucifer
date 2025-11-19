using UnityEngine;

public class MaterialMeleeAI : MonoBehaviour
{
    private Transform playerTransform;

    public void Initialize(Transform player)
    {
        playerTransform = player;
    }
}
