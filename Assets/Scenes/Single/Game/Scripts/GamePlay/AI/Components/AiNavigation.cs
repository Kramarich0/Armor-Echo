using UnityEngine;
using UnityEngine.AI;

public class AINavigation
{
    readonly TankAI owner;

    float smoothedMove = 0f;
    float smoothedTurn = 0f; // normalized -1..1 representing turn command (mapped to degrees internally)
    float moveVelocity = 0f;
    float turnVelocity = 0f;
    float enginePower = 0f;
    float reverseLockTimer = 0f;
    float patrolTimer = 0f;

    public AINavigation(TankAI owner) { this.owner = owner; }

    public void UpdateNavigation()
    {
        if (reverseLockTimer > 0f)
            reverseLockTimer -= Time.deltaTime;

        if (Mathf.Abs(smoothedTurn) < 0.01f) smoothedTurn = 0f;

        float targetMove = GetTargetMoveInput();
        float targetTurn = GetTargetTurnInput();

        float moveSmoothTime = Mathf.Clamp(owner.MoveResponse, 0.02f, 1f);
        float turnSmoothTime = Mathf.Clamp(owner.TurnResponse, 0.02f, 1f);

        smoothedMove = Mathf.SmoothDamp(smoothedMove, targetMove, ref moveVelocity, moveSmoothTime);
        float targetTurnDeg = targetTurn * 180f;
        float smoothedTurnDeg = Mathf.SmoothDampAngle(smoothedTurn * 180f, targetTurnDeg, ref turnVelocity, turnSmoothTime);
        smoothedTurn = Mathf.Clamp(smoothedTurnDeg / 180f, -1f, 1f);

        float inputMagnitude = Mathf.Max(Mathf.Abs(smoothedMove), Mathf.Abs(smoothedTurn));
        enginePower = Mathf.MoveTowards(enginePower, inputMagnitude > 0.01f ? 1f : 0f, Time.deltaTime * 4f);
    }

    public void MoveTo(Vector3 position)
    {
        if (owner == null || owner.agent == null || !owner.navAvailable || !owner.agent.isOnNavMesh) return;

        UpdateNavigation();

        if (!owner.agent.hasPath || Vector3.Distance(owner.agent.destination, position) > 0.5f)
            owner.agent.SetDestination(position);

        var rb = owner.rb;
        bool hasTracks = owner.leftTrack != null && owner.rightTrack != null && rb != null;

        if (!hasTracks)
        {
            owner.agent.updatePosition = true;
            owner.agent.updateRotation = true;
            owner.agent.isStopped = false;
            AlignBodyToVelocity();
            return;
        }

        owner.agent.updatePosition = false;
        owner.agent.updateRotation = false;
        owner.agent.isStopped = false;

        // keep agent internal position synced with physical
        owner.agent.nextPosition = owner.transform.position;

        float stopDist = Mathf.Max(0.25f, owner.agent.stoppingDistance);
        Vector3 toTarget = position - owner.transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < stopDist * stopDist)
        {
            ApplyTankPhysics(0f, 0f, rb);
            owner.agent.isStopped = true;
            return;
        }

        float navAngleBoost = 0f;
        if (owner.agent.hasPath)
        {
            Vector3 toNext = owner.agent.steeringTarget - owner.transform.position;
            toNext.y = 0f;
            if (toNext.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.SignedAngle(owner.transform.forward, toNext.normalized, Vector3.up);
                if (Mathf.Abs(angle) > 35f)
                    navAngleBoost = Mathf.Clamp(angle / 45f, -1f, 1f) * 1.2f;
            }
        }

        float finalTurn = smoothedTurn;
        if (Mathf.Abs(navAngleBoost) > Mathf.Abs(finalTurn))
            finalTurn = Mathf.Lerp(finalTurn, navAngleBoost, 0.7f);

        ApplyTankPhysics(smoothedMove, finalTurn, rb);
        AlignBodyToMovementDirection(smoothedMove, finalTurn);
    }

    private float GetTargetMoveInput()
    {
        if (owner.agent == null || !owner.agent.hasPath) return 0f;

        Vector3 toNext = owner.agent.steeringTarget - owner.transform.position;
        toNext.y = 0f;

        if (toNext.magnitude < 0.1f) return 0f;

        Vector3 dir = toNext.normalized;
        float angle = Vector3.SignedAngle(owner.transform.forward, dir, Vector3.up);

        float moveInput = Vector3.Dot(owner.transform.forward, dir);

        if (Mathf.Abs(angle) > 60f)
            moveInput = Mathf.Clamp(moveInput, -0.6f, 0.6f);

        return Mathf.Clamp(moveInput, -1f, 1f);
    }

    private float GetTargetTurnInput()
    {
        if (owner.agent == null || !owner.agent.hasPath) return 0f;

        Vector3 toNext = owner.agent.steeringTarget - owner.transform.position;
        toNext.y = 0f;

        if (toNext.magnitude < 0.1f) return 0f;

        Vector3 dir = toNext.normalized;
        float angle = Vector3.SignedAngle(owner.transform.forward, dir, Vector3.up);

        float turnInput = Mathf.Clamp(angle / 45f, -1f, 1f);

        if (Mathf.Abs(angle) > 60f)
            turnInput = Mathf.Sign(angle) * Mathf.Lerp(0.9f, 1f, (Mathf.Abs(angle) - 60f) / 120f);

        return turnInput;
    }

    private void ApplyTankPhysics(float moveInput, float turnInput, Rigidbody rb)
    {
        if (rb == null) return;

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, owner.transform.forward);
        float absForwardSpeed = Mathf.Abs(currentForwardSpeed);

        if (absForwardSpeed > owner.MovingThreshold && moveInput != 0f &&
            Mathf.Sign(currentForwardSpeed) != Mathf.Sign(moveInput) && reverseLockTimer <= 0f)
        {
            reverseLockTimer = owner.ReverseLockDuration;
        }

        float desiredBrake = 0f;
        if (reverseLockTimer > 0f)
        {
            desiredBrake = owner.MaxBrakeTorque;
            owner.leftTrack?.ApplyTorque(0f, desiredBrake);
            owner.rightTrack?.ApplyTorque(0f, desiredBrake);
            return;
        }

        float speedFactor = Mathf.Clamp01(absForwardSpeed / Mathf.Max(0.0001f, owner.MaxForwardSpeed));
        float lowSpeedBoost = 1f + (1f - speedFactor) * 2.0f;
        float effectiveTurnSharpness = owner.TurnSharpness * lowSpeedBoost;

        float leftPower = Mathf.Clamp(moveInput + turnInput * effectiveTurnSharpness, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turnInput * effectiveTurnSharpness, -1f, 1f);

        bool wantsReverse = moveInput != 0f && Mathf.Sign(moveInput) != Mathf.Sign(currentForwardSpeed);
        if (absForwardSpeed > 0.5f && wantsReverse)
        {
            float speedRatio = Mathf.InverseLerp(0.5f, owner.MaxForwardSpeed, absForwardSpeed);
            desiredBrake = Mathf.Lerp(owner.MaxBrakeTorque * 0.2f, owner.MaxBrakeTorque, speedRatio);
            leftPower *= 0.2f;
            rightPower *= 0.2f;
        }

        float currentMaxSpeed = currentForwardSpeed > 0f ? owner.MaxForwardSpeed : owner.MaxBackwardSpeed;
        float speedLimitFactor = 1f;
        if (absForwardSpeed > currentMaxSpeed * 0.8f)
        {
            speedLimitFactor = Mathf.InverseLerp(currentMaxSpeed, currentMaxSpeed * 0.8f, absForwardSpeed);
            speedLimitFactor = Mathf.Clamp01(speedLimitFactor);
        }

        float reverseFactor = 0.6f;
        float leftMotor = leftPower * owner.MaxMotorTorque * speedLimitFactor * enginePower * (leftPower < 0f ? reverseFactor : 1f);
        float rightMotor = rightPower * owner.MaxMotorTorque * speedLimitFactor * enginePower * (rightPower < 0f ? reverseFactor : 1f);

        owner.leftTrack?.ApplyTorque(leftMotor, desiredBrake);
        owner.rightTrack?.ApplyTorque(rightMotor, desiredBrake);

        Vector3 right = owner.transform.right;
        float lateral = Vector3.Dot(rb.linearVelocity, right);
        if (lateral * lateral > 0.01f)
            rb.AddForce(-right * lateral * 8f, ForceMode.Acceleration);
    }

    private void AlignBodyToMovementDirection(float moveInput, float turnInput)
    {
        if (owner.body == null) return;

        Vector3 desiredForward = owner.transform.forward;

        if (Mathf.Abs(moveInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredForward);
            owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, targetRotation, owner.RotationSpeed * Time.deltaTime * 40f);
        }
    }

    public void AlignBodyToVelocity()
    {
        if (owner.body != null && owner.agent != null)
        {
            Vector3 vel = owner.agent.velocity;
            if (vel.sqrMagnitude > 0.01f)
            {
                Vector3 forwardDir = vel.normalized * (owner.invertBodyForward ? -1f : 1f);
                Quaternion target = Quaternion.LookRotation(forwardDir);
                owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, target, owner.RotationSpeed * Time.deltaTime * 40f);
            }
        }
    }

    public void PatrolRandom()
    {
        patrolTimer -= Time.deltaTime;
        if (patrolTimer > 0f) return;

        patrolTimer = Random.Range(2f, 4f);
        if (owner.agent != null && owner.navAvailable && owner.agent.isOnNavMesh && !owner.agent.hasPath)
        {
            Vector3 rand = owner.transform.position + Random.insideUnitSphere * 8f;
            if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                owner.agent.SetDestination(hit.position);
        }
    }

    public void DrawGizmos()
    {
        if (owner == null || !owner.debugGizmos) return;
        Gizmos.color = Color.green;
        if (owner.agent != null)
            Gizmos.DrawLine(owner.transform.position, owner.transform.position + owner.agent.velocity);
    }
}
