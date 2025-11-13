using UnityEngine;
using UnityEngine.XR;

public class PlayerSoulState : MonoBehaviour
{
    public enum SoulState
    {
        Enter, // charging up
        Full,  // fully charged
        Idle, // already charged
        Blast, // Soul Blast attack
        Zap, // Soul Zap attack
        Heal // continous healing when fully charged
    }

    public SoulState? currentSoulState = null;

    private void Update()
    {
        if (!ReceivedInputForSoulState())
        {
            HandleNoSoulStateInput();
            return;
        }

        UpdateSoulState();
    }

    private bool ReceivedInputForSoulState()
    {
        if (InputManager.instance.SoulStateInput)
            return true;

        return false;
    }

    private void HandleNoSoulStateInput()
    {
        if (currentSoulState != null)
        {
            // Reset soul state when no input is received
            currentSoulState = null;
        }

        // reset timer, give back Souls and other things
    }

    private void UpdateSoulState()
    {
        
    }
}
