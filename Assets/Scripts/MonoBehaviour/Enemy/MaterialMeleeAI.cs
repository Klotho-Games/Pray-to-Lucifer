using UnityEngine;

public class MaterialMeleeAI : MonoBehaviour
{
    public Transform PlayerTransform { get; private set; }

    public void Initialize(Transform player)
    {
        PlayerTransform = player;
    }
}
