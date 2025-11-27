using UnityEngine;

public class MovementPhysics
{
    readonly Tank owner;
    readonly MovementContext ctx;

    public MovementPhysics(Tank owner, MovementContext ctx)
    {
        this.owner = owner;
        this.ctx = ctx;
    }

    public void HandleMovementPhysics(float moveInput, float turnInput)
    {
        if (owner.rb == null) return;

        Rigidbody rb = owner.rb;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, owner.transform.forward);
        float absSpeed = Mathf.Abs(forwardSpeed);

        if (absSpeed > owner.MovingThreshold &&
            moveInput != 0 &&
            Mathf.Sign(forwardSpeed) != Mathf.Sign(moveInput) &&
            ctx.reverseLockTimer <= 0)
        {
            ctx.reverseLockTimer = owner.ReverseLockDuration;
        }

        float brakeTorque = 0f;

        if (ctx.reverseLockTimer > 0f)
        {
            brakeTorque = owner.MaxBrakeTorque;
            owner.leftTrack?.ApplyTorque(0f, brakeTorque);
            owner.rightTrack?.ApplyTorque(0f, brakeTorque);
            return;
        }

        float speed01 = Mathf.Clamp01(absSpeed / owner.MaxForwardSpeed);
        float lowSpeedTurnBoost = 1f + (1f - speed01) * 1.5f;

        float turn = turnInput * owner.TurnSharpness * lowSpeedTurnBoost;

        float leftPower = Mathf.Clamp(moveInput + turn, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turn, -1f, 1f);

        bool changingDirection = Mathf.Sign(moveInput) != Mathf.Sign(forwardSpeed);

        if (absSpeed > 0.5f && changingDirection)
        {
            float ratio = Mathf.InverseLerp(0.5f, owner.MaxForwardSpeed, absSpeed);
            brakeTorque = Mathf.Lerp(owner.MaxBrakeTorque * 0.2f, owner.MaxBrakeTorque, ratio);

            leftPower *= 0.25f;
            rightPower *= 0.25f;
        }

        float leftMotorForce = leftPower * owner.MaxMotorTorque * ctx.enginePower;
        float rightMotorForce = rightPower * owner.MaxMotorTorque * ctx.enginePower;

        owner.leftTrack?.ApplyTorque(leftMotorForce, brakeTorque);
        owner.rightTrack?.ApplyTorque(rightMotorForce, brakeTorque);

        Vector3 vel = rb.linearVelocity;
        float fwd = Vector3.Dot(vel, owner.transform.forward);

        float maxAllowed = fwd >= 0 ? owner.MaxForwardSpeed : owner.MaxBackwardSpeed;

        if (Mathf.Abs(fwd) > maxAllowed)
        {
            float clamped = Mathf.Clamp(fwd, -owner.MaxBackwardSpeed, owner.MaxForwardSpeed);

            Vector3 forwardPart = owner.transform.forward * clamped;
            Vector3 sidewaysPart = Vector3.Project(vel, owner.transform.right);

            rb.linearVelocity = forwardPart + sidewaysPart;
        }
    }
}
