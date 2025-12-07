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
        Vector3 vel = rb.linearVelocity;

        float enginePower = Mathf.Clamp(ctx.enginePower, 0f, 1f);

        Vector3 forwardDir = owner.transform.forward;
        float forwardSpeed = Vector3.Dot(vel, forwardDir); 
        float absSpeed = Mathf.Abs(forwardSpeed);

        bool changingDirection = moveInput != 0f && Mathf.Sign(moveInput) != Mathf.Sign(forwardSpeed);

        if (absSpeed > owner.MovingThreshold && changingDirection && ctx.reverseLockTimer <= 0f)
        {
            ctx.reverseLockTimer = owner.ReverseLockDuration;
        }

        if (moveInput > 0f && forwardSpeed >= owner.MaxForwardSpeed)
        {
            moveInput = 0f;
        }
        else if (moveInput < 0f && Mathf.Abs(forwardSpeed) >= owner.MaxBackwardSpeed)
        {
            moveInput = 0f;
        }

        float brakeTorque = 0f;

        if (ctx.reverseLockTimer > 0f)
        {
            float ratio = Mathf.Clamp01(absSpeed / owner.MaxForwardSpeed);
            brakeTorque = Mathf.Lerp(0f, owner.MaxBrakeTorque, ratio);

            float leftPower = Mathf.Lerp(0f, moveInput, 0.25f);
            float rightPower = Mathf.Lerp(0f, moveInput, 0.25f);

            owner.leftTrack?.ApplyTorque(leftPower * owner.MaxMotorTorque * enginePower, brakeTorque);
            owner.rightTrack?.ApplyTorque(rightPower * owner.MaxMotorTorque * enginePower, brakeTorque);

            ctx.reverseLockTimer -= Time.fixedDeltaTime;
            return;
        }

        float speed01 = Mathf.Clamp01(absSpeed / owner.MaxForwardSpeed);
        float lowSpeedTurnBoost = 1f + (1f - speed01) * 1.5f;
        float turn = turnInput * owner.TurnSharpness * lowSpeedTurnBoost;

        float leftPowerFinal = Mathf.Clamp(moveInput + turn, -1f, 1f);
        float rightPowerFinal = Mathf.Clamp(moveInput - turn, -1f, 1f);

        if (changingDirection && absSpeed > 0.5f)
        {
            float ratio = Mathf.InverseLerp(0.5f, owner.MaxForwardSpeed, absSpeed);
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

        owner.leftTrack?.ApplyTorque(leftPowerFinal * owner.MaxMotorTorque * enginePower, brakeTorque);
        owner.rightTrack?.ApplyTorque(rightPowerFinal * owner.MaxMotorTorque * enginePower, brakeTorque);

        float currentFwd = Vector3.Dot(rb.linearVelocity, forwardDir);
        float maxAllowed = currentFwd >= 0f ? owner.MaxForwardSpeed : owner.MaxBackwardSpeed;

        if (Mathf.Abs(currentFwd) > maxAllowed && Mathf.Abs(currentFwd) <= maxAllowed * 1.2f)
        {
            float excess = Mathf.Abs(currentFwd) - maxAllowed;
            const float dampingFactor = 10f; 
            rb.AddForce(dampingFactor * excess * -forwardDir, ForceMode.Acceleration);
        }
        else if (Mathf.Abs(currentFwd) > maxAllowed * 1.2f)
        {
            float clamped = Mathf.Sign(currentFwd) * maxAllowed;
            Vector3 forwardPart = forwardDir * clamped;
            Vector3 lateral = Vector3.Project(rb.linearVelocity, owner.transform.right);
            Vector3 vertical = Vector3.Project(rb.linearVelocity, owner.transform.up);
            rb.linearVelocity = forwardPart + lateral + vertical;
        }
    }
}
