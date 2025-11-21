using UnityEngine;
using UnityEngine.AI;

public class AINavigation
{
    readonly TankAI owner;

    float smoothedMove = 0f;
    float smoothedTurn = 0f;
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

        if (Mathf.Abs(smoothedTurn) < 0.05f) smoothedTurn = 0f;

        float targetMove = GetTargetMoveInput();
        float targetTurn = GetTargetTurnInput();

        smoothedMove = Mathf.SmoothDamp(smoothedMove, targetMove, ref moveVelocity, 1f / Mathf.Max(owner.MoveResponse, 0.1f));
        smoothedTurn = Mathf.SmoothDamp(smoothedTurn, targetTurn, ref turnVelocity, 1f / Mathf.Max(owner.TurnResponse, 0.1f));

        float inputMagnitude = Mathf.Max(Mathf.Abs(smoothedMove), Mathf.Abs(smoothedTurn));
        float targetEnginePower = inputMagnitude > 0.01f ? 1f : 0f;
        enginePower = Mathf.MoveTowards(enginePower, targetEnginePower, Time.deltaTime * 0.8f);
    }

    public void MoveTo(Vector3 position)
    {
        if (owner.agent == null || !owner.navAvailable || !owner.agent.isOnNavMesh) return;

        UpdateNavigation();

        if (!owner.agent.hasPath || Vector3.Distance(owner.agent.destination, position) > 0.5f)
            owner.agent.SetDestination(position);

        var rb = owner.rb;
        bool hasTracks = owner.leftTrack != null && owner.rightTrack != null && rb != null;

        if (!hasTracks)
        {
            owner.agent.updatePosition = true;
            owner.agent.isStopped = false;
            AlignBodyToVelocity();
            return;
        }

        owner.agent.updatePosition = false;
        owner.agent.isStopped = false;

        float stopDist = Mathf.Max(0.25f, owner.agent.stoppingDistance);
        Vector3 toTarget = position - owner.transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < stopDist * stopDist)
        {
            ApplyTankPhysics(0f, 0f, rb);
            owner.agent.isStopped = true;
            return;
        }

        ApplyTankPhysics(smoothedMove, smoothedTurn, rb);

        AlignBodyToMovementDirection(smoothedMove, smoothedTurn);
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
            moveInput = Mathf.Clamp(moveInput, -0.3f, 0.3f);

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

        float speedFactor = Mathf.Clamp01(absForwardSpeed / owner.MaxForwardSpeed);
        float lowSpeedBoost = 1f + (1f - speedFactor) * 2.0f;
        float effectiveTurnSharpness = owner.TurnSharpness * lowSpeedBoost;

        float leftPower = Mathf.Clamp(moveInput + turnInput * effectiveTurnSharpness, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turnInput * effectiveTurnSharpness, -1f, 1f);

        bool wantsReverse = Mathf.Sign(moveInput) != Mathf.Sign(currentForwardSpeed);
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

        if (Mathf.Abs(moveInput) > 0.1f || Mathf.Abs(turnInput) > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredForward);
            owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, targetRotation, owner.RotationSpeed * Time.deltaTime);
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
                owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, target, owner.RotationSpeed * Time.deltaTime);
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
        if (!owner.debugGizmos) return;
        Gizmos.color = Color.green;
        if (owner.agent != null)
            Gizmos.DrawLine(owner.transform.position, owner.transform.position + owner.agent.velocity);
    }
}