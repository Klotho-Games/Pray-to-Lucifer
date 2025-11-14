using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

public class PlayerSoulState : MonoBehaviour
{
    /// <summary>
    /// Light properties: intensity, outer radius
    /// </summary>
    public struct Light
    {
        public float intensity;
        public float outerRadius;
    }

    public enum SoulState
    {
        Enter, // charging up
        Full,  // fully charged
        Idle, // already charged
        Heal // continous healing when fully charged
    }

    public SoulState? currentSoulState = null;
    
    [SerializeField] private Animator animator;
    [SerializeField] private Light2D playerLight;

    [Header("Charging")]
    [SerializeField] private float soulChargeTime = 2f;
    [SerializeField] private float fullChargeSoulCost = 100f;

    [Header("Healing")]
    [SerializeField] private float HPAmountPerSecond = 5f;
    [SerializeField] private float soulCostPerHPHealed = 10f;

    [Header("Light Properties")]
    [SerializeField] private Light basicPlayerLight = new() { intensity = 2f, outerRadius = 7f };
    [SerializeField] private Light chargedPlayerLight = new() { intensity = 3f, outerRadius = 9f };
    [SerializeField] private Ease intensityTweenEase = Ease.Linear;
    [SerializeField] private Ease outerRadiusTweenEase = Ease.Linear;

    [Header("Don't mess with these")]


    
    private Tween? intensityTween;
    private Tween? outerRadiusTween;
    private int allocatedSouls = 0;
    private float chargeTimer = 0;
    private bool isZapping = false;

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
            currentSoulState = null;
            CancelCharging();
        }
        
        playerLight.intensity = basicPlayerLight.intensity;
        playerLight.pointLightOuterRadius = basicPlayerLight.outerRadius;
        // reset timer, give back Souls and other things
    }

    private void UpdateSoulState()
    {        
        switch (currentSoulState)
        {
            case null:
                currentSoulState = SoulState.Enter;
                Charge();
                break;

            case SoulState.Enter:

                break;

            case SoulState.Full or SoulState.Idle or SoulState.Heal:
                HandleFullyChargedState();
                break;

            default:
                break;
        }
    }

    private void Charge()
    {
        chargeTimer += Time.deltaTime;

        if (chargeTimer >= soulChargeTime)
        {
            currentSoulState = SoulState.Full;
            chargeTimer = 0;
        }
        else
        {            
            // Tween light intensity
            intensityTween ??= Tween.Custom(basicPlayerLight.intensity, chargedPlayerLight.intensity, 
                soulChargeTime, onValueChange: value => playerLight.intensity = value, intensityTweenEase);
            
            // Tween light outer radius
            outerRadiusTween ??= Tween.Custom(basicPlayerLight.outerRadius, chargedPlayerLight.outerRadius,
                soulChargeTime, onValueChange: value => playerLight.pointLightOuterRadius = value, outerRadiusTweenEase);

            
        }
    }

    private void CancelCharging()
    {
        // return Souls to player
        chargeTimer = 0;

    }

    private void HandleFullyChargedState()
    {

        if (currentSoulState == SoulState.Heal)
            Heal();
    }

    private void Heal()
    {
        
    }

    private void OnSoulBlastAttackPerformed()
    {
        
    }

    private void OnZapAttackStarted()
    {
        
    }

    private void OnZapAttackCanceled()
    {
        currentSoulState = SoulState.Enter;
        isZapping = false;
    }

    private void ZapCancel()
    {
        isZapping = false;
        
    }
}
