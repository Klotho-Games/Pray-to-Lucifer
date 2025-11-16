using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerSoulState : MonoBehaviour
{
    /// <summary>
    /// Light properties: intensity, outer radius
    /// </summary>
    [Serializable]
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
        Heal, // continous healing when fully charged
        Blast // Soul Blast attack
    }

    public SoulState? currentSoulState = null;
    

    [Header("Charging")]
    [SerializeField] private float chargeDuration = 2f;

    [Header("Healing")]
    [SerializeField] private int HPAmountPerSecond = 5;

    [Header("Light Properties")]
    [SerializeField] private Light basicPlayerLight = new() { intensity = 2f, outerRadius = 7f };
    [SerializeField] private Light chargedPlayerLight = new() { intensity = 3f, outerRadius = 9f };
    [SerializeField] private Ease intensityTweenEase = Ease.Linear;
    [SerializeField] private Ease outerRadiusTweenEase = Ease.Linear;

    [Header("Soul Zap Attack")]
    [SerializeField] private float hitRadius = 1f;
    [SerializeField] private GameObject zapIndicator;
    [SerializeField] private float zapIndicatorMoveSpeed = 5f;
    [Tooltip("Speed at which zap indicator is moving while over an enemy, not yet implemented")]
    [SerializeField] private float zapIndicatorMoveOverEnemySpeed = 2f;

    [Header("Don't mess with these")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Animator animator;
    [SerializeField] private Light2D playerLight;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody2D rb;


    
    private int allocatedSouls = 0;
    private float chargeTimer = 0;
    private float soulChargeTimer = 0;
    private float healTimer = 0;
    private bool isZapping = false;
    private Vector2 zapIndicatorDirection = Vector2.zero;

    private void Update()
    {
        if (!ReceivedInputForSoulState())
        {
            HandleNoSoulStateInput();
            return;
        }

        UpdateSoulState();

        if (isZapping)
        {
            ContinueZapAttack();
        }
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
                rb.linearVelocity = Vector2.zero;
                playerMovement.enabled = false;
                Charge();
                break;

            case SoulState.Enter:
                Charge();
                break;

            case SoulState.Full or SoulState.Idle or SoulState.Heal:
                HandleFullyChargedState();
                break;

            case SoulState.Blast:
                SoulBlast();
                allocatedSouls = 0;
                currentSoulState = SoulState.Enter;
                break;

            default:
                break;
        }
    }

    private void Charge()
    {
        if (allocatedSouls >= SoulEconomyManager.instance.FullSoulStateChargeCost)
        {
            currentSoulState = SoulState.Full;
            allocatedSouls = SoulEconomyManager.instance.FullSoulStateChargeCost;
            chargeTimer = 0;
            soulChargeTimer = 0;
        }
        else
        {
            chargeTimer += Time.deltaTime;

            float chargeFraction = Mathf.Clamp01(chargeTimer / chargeDuration);
            
            // Manually interpolate light values using the charge fraction with easing
            float intensityEased = EvaluateEase(chargeFraction, intensityTweenEase);
            playerLight.intensity = Mathf.Lerp(basicPlayerLight.intensity, chargedPlayerLight.intensity, intensityEased);
            
            float outerRadiusEased = EvaluateEase(chargeFraction, outerRadiusTweenEase);
            playerLight.pointLightOuterRadius = Mathf.Lerp(basicPlayerLight.outerRadius, chargedPlayerLight.outerRadius, outerRadiusEased);


            soulChargeTimer += Time.deltaTime;

            if (soulChargeTimer * SoulEconomyManager.instance.FullSoulStateChargeCost / chargeDuration >= 1)
            {
                int soulToAllocate = Mathf.FloorToInt(soulChargeTimer * SoulEconomyManager.instance.FullSoulStateChargeCost / chargeDuration);
                if (allocatedSouls + soulToAllocate > SoulEconomyManager.instance.FullSoulStateChargeCost)
                {
                    soulToAllocate = SoulEconomyManager.instance.FullSoulStateChargeCost - allocatedSouls;
                }
                if (soulToAllocate > playerStats.CurrentSoul)
                {
                    return;
                }
                allocatedSouls += soulToAllocate;
                playerStats.CurrentSoul -= soulToAllocate;
                soulChargeTimer = 0;
            }
        }
    }

    private void HandleFullyChargedState()
    {
        if (currentSoulState == SoulState.Idle)
            return;

        if (currentSoulState == SoulState.Full)
        {
            healTimer = 0;
        }
        
        if (playerStats.CurrentHealth < playerStats.MaxHealth && playerStats.CurrentSoul >= SoulEconomyManager.instance.CostPerHPHealed)
        {
            currentSoulState = SoulState.Heal;
            Heal();
        }
        else
            currentSoulState = SoulState.Idle;
    }

    private void CancelCharging()
    {
        playerStats.CurrentSoul += allocatedSouls;
        if (playerStats.CurrentSoul > playerStats.MaxSoul)
            playerStats.CurrentSoul = playerStats.MaxSoul;
        allocatedSouls = 0;
        chargeTimer = 0;
        rb.linearVelocity = Vector2.zero;
        playerMovement.enabled = true;
        playerLight.intensity = basicPlayerLight.intensity;
        playerLight.pointLightOuterRadius = basicPlayerLight.outerRadius;
    }

    private void Heal()
    {
        healTimer += Time.deltaTime;
        if (healTimer * HPAmountPerSecond >= 1)
        {
            int hpToHeal = Mathf.FloorToInt(healTimer * HPAmountPerSecond);
            int soulNeeded = hpToHeal * SoulEconomyManager.instance.CostPerHPHealed;
            if (playerStats.CurrentSoul >= soulNeeded)
            {
                playerStats.CurrentHealth += hpToHeal;
                playerStats.CurrentSoul -= soulNeeded;
                healTimer = 0;
            }
            else
            {
                int affordableHPToHeal = Mathf.FloorToInt(playerStats.CurrentSoul / (SoulEconomyManager.instance.CostPerHPHealed * 1f));
                playerStats.CurrentHealth += affordableHPToHeal;
                playerStats.CurrentSoul -= affordableHPToHeal * SoulEconomyManager.instance.CostPerHPHealed;
                healTimer = 0;
            }
        }
    }

    private void SoulBlast()
    {
        if (isZapping)
            ZapCancel();
        Debug.Log("Soul Blast!");
    }

    private void SoulZap(Vector2 pos)
    {
        Debug.Log($"Zap at position: {pos}");
        SpawnCircleAtPosition(pos); // Debug purpose
        void SpawnCircleAtPosition(Vector2 position)
        {
            Transform t = Instantiate(zapIndicator, position, Quaternion.identity).transform;
            t.localScale = Vector3.one * hitRadius;
            t.GetComponent<SpriteRenderer>().color = Color.red;
            t.gameObject.SetActive(true);
        }
    }

    private void OnZapAttackStarted()
    {
        if (currentSoulState is null || currentSoulState == SoulState.Blast)
            return;
        if (allocatedSouls <= 0)
            return;
        
        SetIndicatorDirection();
        zapIndicator.transform.position = transform.position;
        zapIndicator.SetActive(true);
        isZapping = true;
    }
    private void SetIndicatorDirection()
    {
        zapIndicatorDirection = (InputManager.instance.IsKeyboardAndMouse ? (InputManager.instance.MousePosition - (Vector2)transform.position) : InputManager.instance.MoveInput).normalized;
    }

    private void ContinueZapAttack()
    {
        if (zapIndicatorDirection != Vector2.zero)
        {
            MoveZapIndicatorInDirection();
        }
        else
        {
            SetIndicatorDirection();
            if (zapIndicatorDirection != Vector2.zero)
                MoveZapIndicatorInDirection();
        }

        void MoveZapIndicatorInDirection()
        {
            zapIndicator.transform.position += (Vector3)(Time.deltaTime * zapIndicatorMoveSpeed * zapIndicatorDirection);
        }
    }

    private void OnZapAttackCanceled() // input cancelled, execute zap
    {
        if (isZapping == false)
            return;

        Vector2 zapIndicatorPos = zapIndicator.transform.position;
        zapIndicator.SetActive(false);
        SoulZap(zapIndicatorPos);

        allocatedSouls = 0;
        currentSoulState = SoulState.Enter;
        isZapping = false;
    }

    private void ZapCancel()
    {
        zapIndicator.SetActive(false);
        isZapping = false;
    }

    void Start()
    {
        InputManager.instance.SecondaryShootAction.started += ctx => OnZapAttackStarted();
        InputManager.instance.SecondaryShootAction.canceled += ctx => OnZapAttackCanceled();
        InputManager.instance.CancelAction.performed += ctx => ZapCancel();
    }

    void OnDestroy()
    {
        InputManager.instance.SecondaryShootAction.started -= ctx => OnZapAttackStarted();
        InputManager.instance.SecondaryShootAction.canceled -= ctx => OnZapAttackCanceled();
        InputManager.instance.CancelAction.performed -= ctx => ZapCancel();
    }

    /// <summary>
    /// Approximates PrimeTween ease evaluation at a given progress (0-1)
    /// </summary>
    public static float EvaluateEase(float progress, Ease ease)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        const float c3 = c1 + 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        const float c5 = (2f * Mathf.PI) / 4.5f;
        
        return ease switch
        {
            Ease.Linear => progress,
            
            // Sine
            Ease.InSine => 1f - Mathf.Cos(progress * Mathf.PI * 0.5f),
            Ease.OutSine => Mathf.Sin(progress * Mathf.PI * 0.5f),
            Ease.InOutSine => -(Mathf.Cos(Mathf.PI * progress) - 1f) * 0.5f,
            
            // Quad
            Ease.InQuad => progress * progress,
            Ease.OutQuad => 1f - (1f - progress) * (1f - progress),
            Ease.InOutQuad => progress < 0.5f ? 2f * progress * progress : 1f - Mathf.Pow(-2f * progress + 2f, 2f) * 0.5f,
            
            // Cubic
            Ease.InCubic => progress * progress * progress,
            Ease.OutCubic => 1f - Mathf.Pow(1f - progress, 3f),
            Ease.InOutCubic => progress < 0.5f ? 4f * progress * progress * progress : 1f - Mathf.Pow(-2f * progress + 2f, 3f) * 0.5f,
            
            // Quart
            Ease.InQuart => progress * progress * progress * progress,
            Ease.OutQuart => 1f - Mathf.Pow(1f - progress, 4f),
            Ease.InOutQuart => progress < 0.5f ? 8f * progress * progress * progress * progress : 1f - Mathf.Pow(-2f * progress + 2f, 4f) * 0.5f,
            
            // Quint
            Ease.InQuint => progress * progress * progress * progress * progress,
            Ease.OutQuint => 1f - Mathf.Pow(1f - progress, 5f),
            Ease.InOutQuint => progress < 0.5f ? 16f * progress * progress * progress * progress * progress : 1f - Mathf.Pow(-2f * progress + 2f, 5f) * 0.5f,
            
            // Expo
            Ease.InExpo => progress == 0f ? 0f : Mathf.Pow(2f, 10f * progress - 10f),
            Ease.OutExpo => progress == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * progress),
            Ease.InOutExpo => progress == 0f ? 0f : progress == 1f ? 1f : progress < 0.5f ? Mathf.Pow(2f, 20f * progress - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * progress + 10f)) * 0.5f,
            
            // Circ
            Ease.InCirc => 1f - Mathf.Sqrt(1f - Mathf.Pow(progress, 2f)),
            Ease.OutCirc => Mathf.Sqrt(1f - Mathf.Pow(progress - 1f, 2f)),
            Ease.InOutCirc => progress < 0.5f ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * progress, 2f))) * 0.5f : (Mathf.Sqrt(1f - Mathf.Pow(-2f * progress + 2f, 2f)) + 1f) * 0.5f,
            
            // Back
            Ease.InBack => c3 * progress * progress * progress - c1 * progress * progress,
            Ease.OutBack => 1f + c3 * Mathf.Pow(progress - 1f, 3f) + c1 * Mathf.Pow(progress - 1f, 2f),
            Ease.InOutBack => progress < 0.5f ? (Mathf.Pow(2f * progress, 2f) * ((c2 + 1f) * 2f * progress - c2)) * 0.5f : (Mathf.Pow(2f * progress - 2f, 2f) * ((c2 + 1f) * (progress * 2f - 2f) + c2) + 2f) * 0.5f,
            
            // Elastic
            Ease.InElastic => progress == 0f ? 0f : progress == 1f ? 1f : -Mathf.Pow(2f, 10f * progress - 10f) * Mathf.Sin((progress * 10f - 10.75f) * c4),
            Ease.OutElastic => progress == 0f ? 0f : progress == 1f ? 1f : Mathf.Pow(2f, -10f * progress) * Mathf.Sin((progress * 10f - 0.75f) * c4) + 1f,
            Ease.InOutElastic => progress == 0f ? 0f : progress == 1f ? 1f : progress < 0.5f ? -(Mathf.Pow(2f, 20f * progress - 10f) * Mathf.Sin((20f * progress - 11.125f) * c5)) * 0.5f : (Mathf.Pow(2f, -20f * progress + 10f) * Mathf.Sin((20f * progress - 11.125f) * c5)) * 0.5f + 1f,
            
            // Bounce
            Ease.InBounce => 1f - EvaluateBounceOut(1f - progress),
            Ease.OutBounce => EvaluateBounceOut(progress),
            Ease.InOutBounce => progress < 0.5f ? (1f - EvaluateBounceOut(1f - 2f * progress)) * 0.5f : (1f + EvaluateBounceOut(2f * progress - 1f)) * 0.5f,
            
            _ => progress // Fallback to linear
        };
    }
    
    public static float EvaluateBounceOut(float progress)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        
        if (progress < 1f / d1)
        {
            return n1 * progress * progress;
        }
        else if (progress < 2f / d1)
        {
            return n1 * (progress -= 1.5f / d1) * progress + 0.75f;
        }
        else if (progress < 2.5f / d1)
        {
            return n1 * (progress -= 2.25f / d1) * progress + 0.9375f;
        }
        else
        {
            return n1 * (progress -= 2.625f / d1) * progress + 0.984375f;
        }
    }
}
