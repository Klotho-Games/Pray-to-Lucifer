using UnityEngine;

public class MaterialProjectileAI : MonoBehaviour
{
    public Transform PlayerTransform { get; private set; }

    public void Initialize(Transform player)
    {
        PlayerTransform = player;
    }
}
