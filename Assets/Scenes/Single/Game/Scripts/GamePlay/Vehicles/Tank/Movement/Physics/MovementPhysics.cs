using UnityEngine;

public class MovementPhysics
{
    readonly TankMovement owner;
    readonly MovementContext ctx;

    public MovementPhysics(TankMovement owner, MovementContext ctx) { this.owner = owner; this.ctx = ctx; }

    public void HandleMovementPhysics(float moveInput, float turnInput)
    {
        if (owner.rb == null) return;

        float currentForwardSpeed = Vector3.Dot(owner.rb.linearVelocity, owner.transform.forward);
        float absForwardSpeed = Mathf.Abs(currentForwardSpeed);

        if (absForwardSpeed > owner.movingThreshold && moveInput != 0f && Mathf.Sign(currentForwardSpeed) != Mathf.Sign(moveInput) && ctx.reverseLockTimer <= 0f)
        {
            ctx.reverseLockTimer = owner.reverseLockDuration;
        }

        float desiredBrake = 0f;
        if (ctx.reverseLockTimer > 0f)
        {
            desiredBrake = owner.maxBrakeTorque;
            owner.leftTrack?.ApplyTorque(0f, desiredBrake);
            owner.rightTrack?.ApplyTorque(0f, desiredBrake);
            return;
        }

        float speedFactor = Mathf.Clamp01(absForwardSpeed / owner.maxForwardSpeed);
        float lowSpeedBoost = 1f + (1f - speedFactor) * 2.0f;
        float effectiveTurnSharpness = owner.turnSharpness * lowSpeedBoost;

        float leftPower = Mathf.Clamp(moveInput + turnInput * effectiveTurnSharpness, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turnInput * effectiveTurnSharpness, -1f, 1f);

        bool wantsReverse = Mathf.Sign(moveInput) != Mathf.Sign(currentForwardSpeed);
        if (absForwardSpeed > 0.5f && wantsReverse)
        {
            float speedRatio = Mathf.InverseLerp(0.5f, owner.maxForwardSpeed, absForwardSpeed);
            desiredBrake = Mathf.Lerp(owner.maxBrakeTorque * 0.2f, owner.maxBrakeTorque, speedRatio);
            leftPower *= 0.2f;
            rightPower *= 0.2f;
        }

        float currentMaxSpeed = currentForwardSpeed > 0f ? owner.maxForwardSpeed : owner.maxBackwardSpeed;
        float speedLimitFactor = 1f;
        if (absForwardSpeed > currentMaxSpeed * 0.8f)
        {
            speedLimitFactor = Mathf.InverseLerp(currentMaxSpeed, currentMaxSpeed * 0.8f, absForwardSpeed);
            speedLimitFactor = Mathf.Clamp01(speedLimitFactor);
        }

        float reverseFactor = 0.6f;
        float leftMotor = leftPower * owner.maxMotorTorque * speedLimitFactor * ctx.enginePower * (leftPower < 0f ? reverseFactor : 1f);
        float rightMotor = rightPower * owner.maxMotorTorque * speedLimitFactor * ctx.enginePower * (rightPower < 0f ? reverseFactor : 1f);

        owner.leftTrack?.ApplyTorque(leftMotor, desiredBrake);
        owner.rightTrack?.ApplyTorque(rightMotor, desiredBrake);
    }
}
