using UnityEngine;

public enum GateTypes
{
    [InspectorName("Mirror")]
    Mirror,

    [InspectorName("One way mirror")]
    One_way_mirror,

    [InspectorName("Diffraction")]
    Diffraction,

    // [InspectorName("Optical fiber")]
    // Optical_fiber,

    [InspectorName("Converging lens")]
    Converging_lens,

    [InspectorName("Lens system")]
    Lens_system,

    [InspectorName("Diverging lens")]
    Diverging_lens
}


[RequireComponent(typeof(Collider2D))]
public class GateType : MonoBehaviour
{
    [Header("Gate Info")]
    [Tooltip("Select the gate type.")]
    public GateTypes gateType = GateTypes.Mirror;
}
