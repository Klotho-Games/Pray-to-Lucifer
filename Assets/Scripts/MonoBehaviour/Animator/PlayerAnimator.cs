using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Soul State Particle Effects")]
    [SerializeField] private ParticleController enterSoulStateParticleController;
    [SerializeField] private ParticleSystem healingParticleSystem;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSoulState soulState;
    private bool wasSoulStateLastFrame = false;


    void Start()
    {
        animator = GetComponent<Animator>();
        animator.SetFloat("inverseOfChargeDuration", 1f / soulState.chargeDuration);
    }

    void Update()
    {
        if (soulState.currentSoulState is not null)
        {
            if (!wasSoulStateLastFrame)
            {
                animator.SetTrigger("startSoulState");
                // begin charging - only start once
                SFXManager.instance.StartLoopingSFX(SFXManager.instance.HealSoulStateSFX, transform.position);

                if (enterSoulStateParticleController != null)
                {
                    enterSoulStateParticleController.gameObject.SetActive(true);
                    enterSoulStateParticleController.PlayAllParticleSystems();
                }
                
                wasSoulStateLastFrame = true;
            }

            animator.SetBool("isSoulState", true);

            if (soulState.currentSoulState == PlayerSoulState.SoulState.Heal)
            {
                // heal state
                if (!healingParticleSystem.isPlaying)
                    healingParticleSystem.Play();
            }
            else
            {
                SFXManager.instance.StopLoopingSFX(SFXManager.instance.HealSFX.Name);
                // charged but not heal state
                if (healingParticleSystem.isPlaying)
                    healingParticleSystem.Stop();
            }
        }
        else
        {
            if (wasSoulStateLastFrame)
            {
                SFXManager.instance.StopLoopingSFX(SFXManager.instance.HealSoulStateSFX.Name);
            }
            
            // stop charging 
            if (healingParticleSystem.isPlaying)
                healingParticleSystem.Stop();

            if (enterSoulStateParticleController != null)
            {
                enterSoulStateParticleController.gameObject.SetActive(false);
            }

            animator.SetBool("isSoulState", false);

            wasSoulStateLastFrame = false;
        }  
    }
    

    /* void DisableOtherParameters(string parameterToKeep)
    {
        
    }

    void SetInt(string parameter, int value)
    {
        animator.SetInteger(parameter, value);
        DisableOtherParameters(parameter);
    } */

    // [SerializeField] private Rigidbody2D rb;
    // [SerializeField] private GameObject BeamController;

    // private const float threshold = 1e-4f;

/*     enum Direction_8
    {
        Back,
        Back_Right,
        Right,
        Forward_Right,
        Forward,
        Forward_Left,
        Left,
        Back_Left
    } */
    /* private void Movement()
    {
        if (Mathf.Abs(rb.linearVelocityX) > threshold || Mathf.Abs(rb.linearVelocityY) > threshold)
        {
            DirectionalMovement();
        }
        else
        {
            if (animator.GetInteger("isIdleFacing") != -1 && BeamController.activeSelf)
            {
                SetInt("isShootingFacing", animator.GetInteger("isIdleFacing"));
                return;
            }
            if (animator.GetInteger("isShootingFacing") != -1 && !BeamController.activeSelf)
            {
                SetInt("isIdleFacing", animator.GetInteger("isShootingFacing"));
                return;
            }
            if (animator.GetInteger("isMovingInDirection") != -1)
            {
                SetInt("isIdleFacing", animator.GetInteger("isMovingInDirection") / 2);
                return;
            }
            
            if (animator.GetInteger("isShootingWhileMovingInDirection") != -1)
            {
                SetInt("isShootingFacing", animator.GetInteger("isShootingWhileMovingInDirection") / 2);
                return;
            }
        }
    }

    void DirectionalMovement()
    {
        if (Mathf.Abs(rb.linearVelocityX) <= threshold && Mathf.Abs(rb.linearVelocityY) <= threshold)
            return;

        if (Mathf.Abs(rb.linearVelocityX) <= threshold)
        {
            VerticalMovement();
            return;
        }

        if (Mathf.Abs(rb.linearVelocityY) <= threshold)
        {
            HorizontalMovement();
            return;
        }

        DiagonalMovement();

        void HorizontalMovement()
        {
            if (rb.linearVelocityX < 0)
            {
                SetDirection(Direction_8.Left);
            }
            else
            {
                SetDirection(Direction_8.Right);
            }
        }

        void VerticalMovement()
        {
            if (rb.linearVelocityY < 0)
            {
                SetDirection(Direction_8.Back);
            }
            else
            {
                SetDirection(Direction_8.Forward);
            }
        }

        void DiagonalMovement()
        {
            // Diagonal Movement
            if (rb.linearVelocityX > 0)
            {
                if (rb.linearVelocityY > 0)
                {
                    SetDirection(Direction_8.Forward_Right);
                }
                else
                {
                    SetDirection(Direction_8.Back_Right);
                }
            }
            else
            {
                if (rb.linearVelocityY > 0)
                {
                    SetDirection(Direction_8.Forward_Left);
                }
                else
                {
                    SetDirection(Direction_8.Back_Left);
                }
            }
        }

        void SetDirection(Direction_8 direction)
        {
            if (BeamController.activeSelf)
            {
                SetInt("isShootingWhileMovingInDirection", (int)direction);
                
            }
            else
            {
                SetInt("isMovingInDirection", (int)direction);
            }
        }
    }*/
}