using UnityEngine;

public class MaterialGrenadeAI : MonoBehaviour
{
    public Transform PlayerTransform { get; private set; }

    public void Initialize(Transform player)
    {
        PlayerTransform = player;
    }
}
