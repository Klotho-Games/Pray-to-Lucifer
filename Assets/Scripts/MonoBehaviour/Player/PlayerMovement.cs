using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    public enum Axis { X, Y }

    [Header("Move")]
    public float maxVelocity = 6.5f;
    public float accelerationDuration = 0.2f;
    public float deccelerationDuration = 0.4f;
    [Range(0, 1f)] public float yAxisModifier = 0.75f;

    private Rigidbody2D rb;

    private float[] moveInput = new float[2];
    private float acceleration;
    private float decceleration;
    private struct MaxVelocityFlags
    {
        public bool x;
        public bool y;
    }
    private MaxVelocityFlags isAtMaxVelocity;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Move(Axis.X);
        Move(Axis.Y);
        
        // Clamp diagonal movement to prevent moving faster diagonally
        ClampVelocityMagnitude();
    }
    
    private void ClampVelocityMagnitude()
    {
        Vector2 velocity = rb.linearVelocity;
        
        // Check if velocity magnitude exceeds maxVelocity
        if (velocity.magnitude > maxVelocity)
        {
            // Normalize and scale back to maxVelocity
            velocity = velocity.normalized * maxVelocity * Mathf.Sqrt(1f + yAxisModifier * yAxisModifier);
            rb.linearVelocity = velocity;
        }
    }

    private void Move(Axis axis)
    {
        int i = (int)axis;
        moveInput[i] = axis == Axis.X ? InputManager.instance.MoveInput.x : InputManager.instance.MoveInput.y * yAxisModifier;
        acceleration = maxVelocity / accelerationDuration;
        decceleration = maxVelocity / deccelerationDuration;

        bool atMaxVelocity = axis == Axis.X ? isAtMaxVelocity.x : isAtMaxVelocity.y;

        if (atMaxVelocity && ReceivedInput(moveInput[i]) && InputInDirectionOfVelocity(axis))
        {
            KeepMoving(axis);
        }
        else
        {
            if (ReceivedInput(moveInput[i]))
            {
                Accelerate(axis);
            }
            else
            {
                Deccelerate(axis);
            }
        }
    }

    private void Accelerate(Axis axis)
    {
        int i = (int)axis;
        Vector2 velocity = rb.linearVelocity;
        if (axis == Axis.X)
            velocity.x += moveInput[i] * acceleration;
        else
            velocity.y += moveInput[i] * acceleration;

        if (Mathf.Abs(axis == Axis.X ? velocity.x : velocity.y) > maxVelocity)
        {
            if (axis == Axis.X)
                isAtMaxVelocity.x = true;
            else
                isAtMaxVelocity.y = true;
            KeepMoving(axis);
        }
        else
        {
            rb.linearVelocity = velocity;
        }
    }

    private void Deccelerate(Axis axis)
    {
        int i = (int)axis;
        Vector2 velocity = rb.linearVelocity;
        float sign = axis == Axis.X ? Mathf.Sign(velocity.x) : Mathf.Sign(velocity.y);

        if (axis == Axis.X)
            velocity.x -= (int)sign * decceleration;
        else
            velocity.y -= (int)sign * decceleration;

        bool signChanged = (int)(axis == Axis.X ? Mathf.Sign(velocity.x) : Mathf.Sign(velocity.y)) != (int)sign;

        if (signChanged)
        {
            if (axis == Axis.X)
                rb.linearVelocity = new Vector2(0, velocity.y);
            else
                rb.linearVelocity = new Vector2(velocity.x, 0);
        }
        else
        {
            rb.linearVelocity = velocity;
            if (axis == Axis.X)
                isAtMaxVelocity.x = false;
            else
                isAtMaxVelocity.y = false;
        }
    }

    private void KeepMoving(Axis axis)
    {
        int i = (int)axis;
        Vector2 velocity = rb.linearVelocity;
        if (axis == Axis.X)
            velocity.x = moveInput[i] * maxVelocity;
        else
            velocity.y = moveInput[i] * maxVelocity;
        rb.linearVelocity = velocity;
    }

    private bool ReceivedInput(float input)
    {
        return input != 0;
    }

    private bool InputInDirectionOfVelocity(Axis axis)
    {
        int i = (int)axis;
        float velocity = axis == Axis.X ? rb.linearVelocity.x : rb.linearVelocity.y;
        return (int)Mathf.Sign(velocity) == (int)Mathf.Sign(moveInput[i]);
    }
}