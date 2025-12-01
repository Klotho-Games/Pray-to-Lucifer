using UnityEngine;

/// <summary>
/// Example clickable 2D object that works with the hover system.
/// </summary>
[RequireComponent(typeof(HighlightableElement2D))]
[RequireComponent(typeof(Collider2D))]
public class Clickable2D : MonoBehaviour, IClickable {
    enum ButtonSpecialFunction {
        None,
        InvokeRotationMode,
        PlayGame,
        USETHISFORSMTHELSE,
        PlayTutorial,
        CloseGame,
        DestroyGate,
        PlaceGate
    }
    [Header("Click Settings")]
    //[SerializeField] private AudioClip clickSound;
    [SerializeField] private ButtonSpecialFunction specialFunction = ButtonSpecialFunction.None;
    [SerializeField] private bool enableDebug = false;
    
    private AudioSource audioSource;
    
    /* void Awake() {
        // Setup audio source if we have a click sound
        if (clickSound != null) {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    } */
    
    public virtual void OnClick() {
        if (enableDebug) {
            if (enableDebug) Debug.Log($"Clicked 2D object: {gameObject.name}");
        }
        
        /* // Play click sound
        if (clickSound != null && audioSource != null) {
            audioSource.PlayOneShot(clickSound);
        } */
        
        // Override this method in derived classes for custom click behavior
        HandleCustomClick();
    }
    
    /// <summary>
    /// Override this method to add custom click behavior.
    /// </summary>
    protected virtual void HandleCustomClick() 
    {
        switch (specialFunction)
        {
            case ButtonSpecialFunction.None:
                // No special function, do nothing
                break;

            case ButtonSpecialFunction.InvokeRotationMode:
                Vector2 cellWorldPos = transform.position;
                GatePlacementManager.instance.EnterGateRotationMode(cellWorldPos);
                break;

            case ButtonSpecialFunction.PlayGame:
                SFXManager.instance.PlaySFX(SFXManager.instance.PlayButtonSFX, transform.position);
                InputManager.instance.CloseMainMenu();
                LevelManager.instance.StartGame();
                break;

            case ButtonSpecialFunction.PlayTutorial:
                SFXManager.instance.PlaySFX(SFXManager.instance.TutorialButtonSFX, transform.position);
                InputManager.instance.CloseMainMenu();
                LevelManager.instance.StartTutorial();
                break;

            case ButtonSpecialFunction.CloseGame:
                SFXManager.instance.PlaySFX(SFXManager.instance.ExitButtonSFX, transform.position);
                Application.Quit();
                break;

            case ButtonSpecialFunction.DestroyGate:
                SFXManager.instance.PlaySFX(SFXManager.instance.DestroyGateSFX, transform.position);
                Vector2 cellWorldPosition = transform.position;
                DestroyGateAtPosition(cellWorldPosition);
                break;

            case ButtonSpecialFunction.PlaceGate:
                SFXManager.instance.PlaySFX(SFXManager.instance.PlaceGateSFX, transform.position);
                gameObject.SetActive(false);
                GatePlacementManager.instance.PlaceGate();
                break;
        }

        
        // Add your custom behavior here
        if (enableDebug) Debug.Log($"Add custom click behavior for {gameObject.name}");
    }

    
    private void DestroyGateAtPosition(Vector2 position)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(position, LayerMask.GetMask("Gate"));
        foreach (var col in colliders)
        {
            if (col.CompareTag("Gate"))
            {
                Destroy(col.gameObject);
                return;
            }
        }
    } 
}