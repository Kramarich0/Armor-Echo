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
        Vector3 forwardDir = owner.transform.forward;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, forwardDir);
        float absSpeed = Mathf.Abs(forwardSpeed);

        bool changingDirection = moveInput != 0f && Mathf.Sign(moveInput) != Mathf.Sign(forwardSpeed);

        if (changingDirection && absSpeed > owner.MovingThreshold && ctx.reverseLockTimer <= 0f)
        {
            ctx.reverseLockTimer = owner.ReverseLockDuration;
        }

        float brakeTorque = 0f;
        float leftPower = moveInput;
        float rightPower = moveInput;

        if (ctx.reverseLockTimer > 0f)
        {
            float ratio = Mathf.Clamp01(absSpeed / owner.MaxForwardSpeed);
            brakeTorque = Mathf.Lerp(0f, owner.MaxBrakeTorque, ratio);

            leftPower *= 0.25f;
            rightPower *= 0.25f;

            owner.leftTrack?.ApplyTorque(leftPower * owner.MaxMotorTorque * ctx.enginePower, brakeTorque);
            owner.rightTrack?.ApplyTorque(rightPower * owner.MaxMotorTorque * ctx.enginePower, brakeTorque);

            ctx.reverseLockTimer -= Time.fixedDeltaTime;
            return;
        }

        float speed01 = Mathf.Clamp01(absSpeed / owner.MaxForwardSpeed);
        float lowSpeedTurnBoost = 1f + (1f - speed01) * 1.5f;
        float turn = turnInput * owner.TurnSharpness * lowSpeedTurnBoost;

        float leftPowerFinal = Mathf.Clamp(moveInput + turn, -1f, 1f);
        float rightPowerFinal = Mathf.Clamp(moveInput - turn, -1f, 1f);

        if (changingDirection && absSpeed > 0.1f)
        {
            float ratio = Mathf.InverseLerp(0.1f, owner.MaxForwardSpeed, absSpeed);
            brakeTorque = Mathf.Lerp(owner.MaxBrakeTorque * 0.2f, owner.MaxBrakeTorque, ratio);

            leftPowerFinal *= 0.5f;
            rightPowerFinal *= 0.5f;
        }

        if ((forwardSpeed > 0f && forwardSpeed >= owner.MaxForwardSpeed && leftPowerFinal > 0f && rightPowerFinal > 0f) ||
            (forwardSpeed < 0f && Mathf.Abs(forwardSpeed) >= owner.MaxBackwardSpeed && leftPowerFinal < 0f && rightPowerFinal < 0f))
        {
            leftPowerFinal = 0f;
            rightPowerFinal = 0f;
        }

        owner.leftTrack?.ApplyTorque(leftPowerFinal * owner.MaxMotorTorque * ctx.enginePower, brakeTorque);
        owner.rightTrack?.ApplyTorque(rightPowerFinal * owner.MaxMotorTorque * ctx.enginePower, brakeTorque);

        float maxAllowed = forwardSpeed >= 0f ? owner.MaxForwardSpeed : owner.MaxBackwardSpeed;

        if (Mathf.Abs(forwardSpeed) > maxAllowed)
        {
            float excess = Mathf.Abs(forwardSpeed) - maxAllowed;
            const float dampingFactor = 8f; 
            rb.AddForce(-forwardDir * excess * dampingFactor, ForceMode.Acceleration);

            if (Mathf.Abs(forwardSpeed) > maxAllowed * 1.2f)
            {
                float clamped = Mathf.Sign(forwardSpeed) * maxAllowed;
                Vector3 lateral = Vector3.Project(rb.linearVelocity, owner.transform.right);
                Vector3 vertical = Vector3.Project(rb.linearVelocity, owner.transform.up);
                rb.linearVelocity = forwardDir * clamped + lateral + vertical;
            }
        }
    }
}
