/// <summary>
/// InputManager handles player input through Unity's Input System.
/// It manages move and button inputs, and provides them through properties.
/// Uses the singleton pattern to ensure only one instance exists.
/// </summary>
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    public bool EnableDebug = false;
    public static InputManager instance;

    [SerializeField] private Camera cam;

    private PlayerInput playerInput;

    public Vector2 MoveInput { get; private set; }
    public bool SoulStateInput { get; private set; }
    public bool PrimaryShootInput { get; private set; } // beam attack
    public bool TertiaryShootInput { get; private set; } // Soul Blast attack

    private InputAction moveAction;
    private InputAction soulStateAction;
    private InputAction primaryShootAction; // beam attack
    public InputAction SecondaryShootAction { get; private set; } // Zap Blast attack
    private InputAction tertiaryShootAction; // Soul Blast attack
    private InputAction dashAction;
    public InputAction CancelAction {get; private set; }

    private void Awake()
    {
        if (EnableDebug) Debug.Log("[InputManager] Awake called");

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (EnableDebug) Debug.Log("[InputManager] Instance created");
        }
        else
        {
            if (EnableDebug) Debug.Log("[InputManager] Instance already exists, destroying duplicate");
            Destroy(gameObject);
        }

        playerInput = GetComponent<PlayerInput>();
        ActionsSetup();
        if (EnableDebug) Debug.Log("[InputManager] Actions setup completed");
    }

    void Start()
    {
        if (EnableDebug) Debug.Log("[InputManager] Start called");
        if (cam == null)
        {
            cam = Camera.main;
            if (EnableDebug) Debug.Log("[InputManager] Camera not assigned, using main camera");
        }
    }

    void OnDestroy()
    {
        if (EnableDebug) Debug.Log("[InputManager] OnDestroy called");
    }

    private void Update()
    {
        UpdateInputs();
    }

    private void UpdateInputs()
    {
        MoveInput = moveAction.ReadValue<Vector2>();
        SoulStateInput = soulStateAction.ReadValue<bool>();
        PrimaryShootInput = primaryShootAction.ReadValue<bool>();
        TertiaryShootInput = tertiaryShootAction.ReadValue<bool>();
        dashAction.ReadValue<bool>();
        

        if (EnableDebug && MoveInput != Vector2.zero)
            Debug.Log($"[InputManager] Move input detected: {MoveInput}");
    }

    private void ActionsSetup()
    {
        if (EnableDebug) Debug.Log("[InputManager] Setting up actions");
        moveAction = playerInput.actions["Move"];
        soulStateAction = playerInput.actions["SoulState"];
        primaryShootAction = playerInput.actions["PrimaryShoot"];
        SecondaryShootAction = playerInput.actions["SecondaryShoot"];
        tertiaryShootAction = playerInput.actions["TertiaryShoot"];
        dashAction = playerInput.actions["Dash"];
        CancelAction = playerInput.actions["Cancel"];
    }
}
