/// <summary>
/// InputManager handles player input through Unity's Input System.
/// It manages move and button inputs, and provides them through properties.
/// Uses the singleton pattern to ensure only one instance exists.
/// </summary>
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    public bool EnableDebug = false;
    public static InputManager instance;

    [SerializeField] private Camera cam;

    private PlayerInput playerInput;

    public bool IsKeyboardAndMouse
    {
        get
        {
            return playerInput.currentControlScheme == "Keyboard&Mouse";
        }
    }

    public Vector2 MoveInput { get; private set; }
    public bool SoulStateInput { get; private set; }
    public bool PrimaryShootInput { get; private set; } // beam attack
    public bool TertiaryShootInput { get; private set; } // Soul Blast attack
    public bool DashInput { get; private set; }
    public Vector2 MousePosition { get; private set; }
    public Vector2 RightStick { get; private set; }
    public bool PreciseControlInput { get; private set; }

    private InputAction moveAction;
    private InputAction soulStateAction;
    private InputAction primaryShootAction; // beam attack
    public InputAction SecondaryShootAction { get; private set; } // Zap Blast attack
    private InputAction tertiaryShootAction; // Soul Blast attack
    private InputAction dashAction;
    public InputAction CancelAction {get; private set; }
    private InputAction mousePositionAction;
    private InputAction rightStickAction;
    private InputAction preciseControlAction;
    public InputAction ItemRightAction {get; private set; } // for cycling through items
    public InputAction ItemLeftAction {get; private set; } // for cycling through items
    private InputAction MenuAction; // for opening menu

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
        Time.timeScale = 1f;
        
        OpenMainMenu(true);
    }

    #region MainMenu
    [Header("Main Menu")]
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private TMP_Text pressMToResumeText;
    [SerializeField] private string startPressMToResumeText;
    [SerializeField] private TMP_Text controlsText;
    [SerializeField] private string startControlsText;

    private float timeScaleBeforeMenu;
    private bool isMenuOpen = false;

    private void OnMenuActionPerformed(InputAction.CallbackContext ctx)
    {
        if (isMenuOpen)
        {
            CloseMainMenu();
        }
        else
        {
            OpenMainMenu();
        }
    }

    private void OpenMainMenu(bool fromStart = false)
    {
        if (isMenuOpen) return;

        timeScaleBeforeMenu = Time.timeScale;
        isMenuOpen = true;

        MainMenu.SetActive(true);
        if (fromStart)
        {
            pressMToResumeText.text = startPressMToResumeText;
            controlsText.text = startControlsText;
        }
        Time.timeScale = 0f;
    }

    public void CloseMainMenu()
    {
        if (!isMenuOpen) return;

        isMenuOpen = false;
        MainMenu.SetActive(false);
        Time.timeScale = timeScaleBeforeMenu;
    }
    #endregion

    void OnDestroy()
    {
        if (EnableDebug) Debug.Log("[InputManager] OnDestroy called");
        
        // Unsubscribe from menu action
        if (MenuAction != null)
        {
            MenuAction.performed -= OnMenuActionPerformed;
        }
    }

    private void Update()
    {
        UpdateInputs();

        if (PrimaryShootInput)
        {
            BeamController.instance.gameObject.SetActive(true);
        }
        else
        {
            BeamController.instance.gameObject.SetActive(false);
        }
    }

    private void UpdateInputs()
    {
        MoveInput = moveAction.ReadValue<Vector2>();
        SoulStateInput = soulStateAction.IsPressed();
        PrimaryShootInput = primaryShootAction.IsPressed();
        TertiaryShootInput = tertiaryShootAction.IsPressed();
        DashInput = dashAction.IsPressed();
        MousePosition = mousePositionAction.ReadValue<Vector2>();
        MousePosition = cam.ScreenToWorldPoint(MousePosition);
        RightStick = rightStickAction.ReadValue<Vector2>();
        PreciseControlInput = preciseControlAction.IsPressed();
        
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
        mousePositionAction = playerInput.actions["MousePosition"];
        rightStickAction = playerInput.actions["RightStick"];
        preciseControlAction = playerInput.actions["PreciseControl"];
        ItemRightAction = playerInput.actions["ItemRight"];
        ItemLeftAction = playerInput.actions["ItemLeft"];
        MenuAction = playerInput.actions["Menu"];
        
        // Subscribe to menu action once during setup
        MenuAction.performed += OnMenuActionPerformed;
    }
}
