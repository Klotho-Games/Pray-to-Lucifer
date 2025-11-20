using UnityEngine;

public class MaterialGrenadeAI : MonoBehaviour
{
    private Transform playerTransform;

    public void Initialize(Transform player)
    {
        playerTransform = player;
    }
}
