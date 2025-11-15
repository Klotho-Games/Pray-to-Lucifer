using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GameObject BeamController;

    private const float threshold = 1e-4f;

    enum Direction_8
    {
        Back,
        Back_Right,
        Right,
        Forward_Right,
        Forward,
        Forward_Left,
        Left,
        Back_Left
    }

    void Update()
    {
        if (SoulState())
            return;

        Movement();
    }
    
    private bool SoulState()
    {

        return false;
    }

    private void Movement()
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
    }

    void DisableOtherParameters(string parameterToKeep)
    {
        if (parameterToKeep != "isMovingInDirection")
            animator.SetInteger("isMovingInDirection", -1);
        if (parameterToKeep != "isIdleFacing")
            animator.SetInteger("isIdleFacing", -1);
        if (parameterToKeep != "isShootingWhileMovingInDirection")
            animator.SetInteger("isShootingWhileMovingInDirection", -1);
        if (parameterToKeep != "isShootingFacing")
            animator.SetInteger("isShootingFacing", -1);
    }

    void SetInt(string parameter, int value)
    {
        animator.SetInteger(parameter, value);
        DisableOtherParameters(parameter);
    }
}