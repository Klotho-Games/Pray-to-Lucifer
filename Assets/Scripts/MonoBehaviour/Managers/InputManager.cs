/// <summary>
/// InputManager handles player input through Unity's Input System.
/// It manages move inputs and click positions, and provides them through properties.
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
    /* public Vector2 ClickWorldPosition { get; private set; }
    private Vector2? lastClickPos; */

    private InputAction moveAction;
    private InputAction soulStateAction;
    /* public InputAction shortClickAction;  // For instant click (press and release)
    public InputAction pressAction;       // For press start */
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

        //SubscribeToClickActions();
    }

    void OnDestroy()
    {
        if (EnableDebug) Debug.Log("[InputManager] OnDestroy called");
        // UnsubscribeFromClickActions();
    }

    /* private void SubscribeToClickActions()
    {
        if (EnableDebug) Debug.Log("[InputManager] Subscribing to click actions");
        if (shortClickAction != null)
        {
            shortClickAction.started += OnClickOrPress;
            if (EnableDebug) Debug.Log("[InputManager] Subscribed to shortClick action");
        }
        if (pressAction != null)
        {
            pressAction.started += OnClickOrPress;
            if (EnableDebug) Debug.Log("[InputManager] Subscribed to press action");
        }
    }

    private void UnsubscribeFromClickActions()
    {
        if (EnableDebug) Debug.Log("[InputManager] Unsubscribing from click actions");
        if (shortClickAction != null)
        {
            shortClickAction.started -= OnClickOrPress;
            if (EnableDebug) Debug.Log("[InputManager] Unsubscribed from shortClick action");
        }
        if (pressAction != null)
        {
            pressAction.started -= OnClickOrPress;
            if (EnableDebug) Debug.Log("[InputManager] Unsubscribed from press action");
        }
    } */

    private void Update()
    {
        UpdateInputs();
    }

    private void UpdateInputs()
    {
        MoveInput = moveAction.ReadValue<Vector2>();
        soulStateAction.ReadValue<bool>();
        if (EnableDebug && MoveInput != Vector2.zero)
            Debug.Log($"[InputManager] Move input detected: {MoveInput}");

        /* if (pressAction?.activeControl?.device is Pointer pointer)
        {
            ProcessPointerPosition(pointer.position.ReadValue());
            if (EnableDebug) Debug.Log($"[InputManager] Processing click from {pressAction.name} action");
        } */
    }

    /* private void OnClickOrPress(InputAction.CallbackContext context)
    {
        if (context.action?.activeControl?.device is Pointer pointer)
        {
            ProcessPointerPosition(pointer.position.ReadValue());
            if (EnableDebug) Debug.Log($"[InputManager] Processing click from {context.action.name} action");
        }
    }

    private void ProcessPointerPosition(Vector2 screenPosition)
    {
        if (cam == null) return;
        
        if (screenPosition != Vector2.positiveInfinity)
        {
            var pos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
            ClickWorldPosition = new(pos.x, pos.y);
            lastClickPos = ClickWorldPosition;
        }
        else
        {
            ClickWorldPosition = Vector2.positiveInfinity;
        }

        if (EnableDebug) Debug.Log($"[InputManager] Click position updated: {ClickWorldPosition}");
    } */

    private void ActionsSetup()
    {
        if (EnableDebug) Debug.Log("[InputManager] Setting up actions");
        moveAction = playerInput.actions["Move"];
        soulStateAction = playerInput.actions["SoulState"];
        /* shortClickAction = playerInput.actions["ShortClick"];
        pressAction = playerInput.actions["Click"];

        // Make sure both actions are enabled
        shortClickAction?.Enable();
        pressAction?.Enable();

        if (EnableDebug && shortClickAction != null)
            Debug.Log($"[InputManager] ShortClick action bound to: {shortClickAction.bindings[0].path}"); */
    }
    
    /* private void OnDrawGizmos()
    {
        if (!EnableDebug || !lastClickPos.HasValue) return;

        Vector2 pos = lastClickPos.Value;
        float size = 0.2f;  // Size of the X
        
        // Set the color for the X
        Gizmos.color = Color.red;
        
        // Draw the X
        Gizmos.DrawLine(new Vector3(pos.x - size, pos.y - size, 0), 
                       new Vector3(pos.x + size, pos.y + size, 0));
        Gizmos.DrawLine(new Vector3(pos.x - size, pos.y + size, 0), 
                       new Vector3(pos.x + size, pos.y - size, 0));
    } */
}
