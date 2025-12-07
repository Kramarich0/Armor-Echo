using Serilog;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIStateHandler
{
    readonly TankAI owner;
    readonly AIPerception perception;
    readonly AINavigation navigation;
    readonly AICombat combat;
    readonly AIWeapons weapons;

    float decisionTimer;
    float minDecisionInterval = 2.5f;
    float maxDecisionInterval = 6f;

    Vector3 currentTacticalPosition;
    bool hasTacticalPosition = false;
    Tactic currentTactic = Tactic.None;

    const float crowdCheckRadius = 3f;

    float microMoveTimer = 0f;
    const float microMoveInterval = 1.2f;
    const float microMoveRadius = 0.9f;

    public AIStateHandler(TankAI owner, AIPerception perception, AINavigation navigation, AICombat combat, AIWeapons weapons)
    {
        this.owner = owner;
        this.perception = perception;
        this.navigation = navigation;
        this.combat = combat;
        this.weapons = weapons;
        decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval);
    }

    public void UpdateState()
    {
        if (owner == null) return;
        perception?.UpdatePerception();

        owner.strafePhase = (owner.strafePhase + Time.deltaTime * Mathf.Max(0.0001f, owner.StrafeSpeed) * (1f + (Mathf.Sin(Time.time * 0.27f) * 0.06f))) % 1f;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            MakeTacticalDecision();
            decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval);
        }

        Transform perceivedTarget = DetermineTarget(out AIState nextState);
        owner.currentState = nextState;

        Transform engageTarget = perceivedTarget;

        if (nextState == AIState.Fighting && engageTarget != null)
        {
            combat?.AimAt(engageTarget);
            TryShootAt(engageTarget);
            PerformTacticBehavior(engageTarget);
        }
        else if (engageTarget != null)
        {
            hasTacticalPosition = false;
            navigation?.MoveTo(engageTarget.position);
            UpdateAgentDefaultMovement(engageTarget.position);
        }
        else
        {
            if (owner.currentCapturePointTarget != null)
            {
                hasTacticalPosition = false;
                navigation?.MoveTo(owner.currentCapturePointTarget.transform.position);
            }
            else
            {
                navigation?.PatrolRandom();
            }
        }

        if (hasTacticalPosition && Vector3.Distance(owner.transform.position, currentTacticalPosition) < Mathf.Max(1f, owner.StrafeRadius * 0.35f))
        {
            microMoveTimer -= Time.deltaTime;
            if (microMoveTimer <= 0f)
            {
                microMoveTimer = microMoveInterval * Random.Range(0.9f, 1.3f);
                Vector3 jitter = Random.insideUnitSphere * microMoveRadius;
                jitter.y = 0f;
                Vector3 microTarget = currentTacticalPosition + jitter;
                if (Vector3.Distance(microTarget, owner.transform.position) > 0.8f)
                {
                    bool skipMicro = false;
                    if (owner.agent != null && owner.agent.hasPath)
                    {
                        if (owner.agent.remainingDistance > Mathf.Max(0.8f, owner.StrafeRadius * 0.4f))
                            skipMicro = true;
                    }
                    if (!skipMicro)
                        navigation?.MoveTo(microTarget);
                }
            }
        }
    }

    void TryShootAt(Transform target)
    {
        if (Time.time < owner.nextFireTime) return;
        if (owner.gunEnd == null) return;

        bool los = perception != null && perception.HasLineOfSight(target);
        Vector3 aimDir = (target.position - owner.gunEnd.position).normalized;
        float angle = Vector3.Angle(owner.gunEnd.forward, aimDir);
        float shootAngleThreshold = owner.enableStrafeWhileShooting ? 36f : 26f;

        if (los && angle < shootAngleThreshold)
        {
            weapons?.ShootAt(target);
            owner.nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, owner.FireRate);
        }
    }

    void PerformTacticBehavior(Transform target)
    {
        if (!hasTacticalPosition || float.IsInfinity(currentTacticalPosition.x))
        {
            currentTacticalPosition = PickBattlePositionNearTarget(target);
            hasTacticalPosition = true;
            currentTactic = Tactic.Strafe;
        }

        switch (currentTactic)
        {
            case Tactic.Strafe:
            {
                float dist = Vector3.Distance(owner.transform.position, currentTacticalPosition);
                if (dist < Mathf.Max(1f, owner.StrafeRadius * 0.4f))
                {
                    if (owner.agent != null) owner.agent.stoppingDistance = Mathf.Max(0.3f, owner.ShootRange * 0.06f);
                }
                else
                {
                    navigation?.MoveTo(currentTacticalPosition);
                }
                UpdateAgentDefaultMovement(currentTacticalPosition);
                break;
            }
            case Tactic.Flank:
            {
                navigation?.MoveTo(currentTacticalPosition);
                if (owner.agent != null) owner.agent.stoppingDistance = Mathf.Clamp(owner.ShootRange * 0.12f, 0.3f, owner.ShootRange * 0.4f);
                UpdateAgentDefaultMovement(currentTacticalPosition);
                break;
            }
            case Tactic.Retreat:
            {
                navigation?.MoveTo(currentTacticalPosition);
                if (owner.agent != null) owner.agent.stoppingDistance = Mathf.Max(0.3f, owner.ShootRange * 0.05f);
                UpdateAgentDefaultMovement(currentTacticalPosition);
                break;
            }
            case Tactic.Capture:
            {
                if (owner.currentCapturePointTarget != null)
                    navigation?.MoveTo(owner.currentCapturePointTarget.transform.position);
                UpdateAgentDefaultMovement(owner.currentCapturePointTarget != null ? owner.currentCapturePointTarget.transform.position : owner.transform.position);
                break;
            }
            default:
            {
                float dist = Vector3.Distance(owner.transform.position, target.position);
                if (dist < owner.ShootRange * 0.6f)
                    navigation?.MoveTo(owner.transform.position + (owner.transform.position - target.position).normalized * (owner.ShootRange * 0.6f));
                else
                    navigation?.MoveTo(target.position);
                UpdateAgentDefaultMovement(target.position);
                break;
            }
        }
    }

    void MakeTacticalDecision()
    {
        Transform enemy = owner.currentTarget;
        Transform cap = owner.currentCapturePointTarget != null ? owner.currentCapturePointTarget.transform : null;

        if (enemy != null)
        {
            float dist = Vector3.Distance(owner.transform.position, enemy.position);

            if (dist < owner.ShootRange * 0.35f && Random.value < 0.7f)
            {
                Vector3 back = owner.transform.position + (owner.transform.position - enemy.position).normalized * Mathf.Clamp(owner.StrafeRadius * 2.0f, 4f, owner.ShootRange * 0.7f);
                currentTacticalPosition = FindFreeNearbyPoint(back, owner.StrafeRadius * 0.6f);
                currentTactic = Tactic.Retreat;
                hasTacticalPosition = true;
                return;
            }

            if (IsAllyClusteredNear(enemy.position, 4f) && Random.value < 0.6f)
            {
                currentTacticalPosition = PickFlankPosition(enemy);
                currentTactic = Tactic.Flank;
                hasTacticalPosition = true;
                return;
            }

            currentTacticalPosition = PickApproachOrStrafe(enemy);
            currentTactic = Tactic.Strafe;
            hasTacticalPosition = true;
            return;
        }

        if (cap != null)
        {
            currentTactic = Tactic.Capture;
            hasTacticalPosition = false;
            return;
        }

        currentTactic = Tactic.None;
        hasTacticalPosition = false;
    }

    bool IsAllyClusteredNear(Vector3 pos, float radius) => CountNearbyAllies(pos) >= 2;

    Vector3 PickApproachOrStrafe(Transform target)
    {
        if (target == null) return owner.transform.position;

        float distToOwner = Vector3.Distance(owner.transform.position, target.position);
        float preferredClose = Mathf.Clamp(owner.ShootRange * 0.5f, 4f, 12f);

        if (distToOwner > preferredClose * 1.2f)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 rnd = Random.insideUnitCircle.normalized * preferredClose * Random.Range(0.9f, 1.1f);
                Vector3 cand = target.position + new Vector3(rnd.x, 0f, rnd.y);

                Vector3 toCand = cand - target.position;
                if (toCand.sqrMagnitude < 0.01f) continue;
                float dot = Vector3.Dot(target.forward.normalized, toCand.normalized);
                if (dot > 0.5f) continue;

                if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    cand = hit.position;
                    if (IsPositionFree(cand, crowdCheckRadius)) return cand;
                }
            }
        }

        return PickBattlePositionNearTarget(target);
    }

    Vector3 PickBattlePositionNearTarget(Transform target)
    {
        if (target == null) return owner.transform.position;

        int attempts = 10;
        float radiusBase = Mathf.Max(2f, owner.StrafeRadius);
        List<Vector3> candidates = new List<Vector3>(attempts);

        for (int i = 0; i < attempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = radiusBase * Random.Range(0.85f, 1.35f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 cand = target.position + dir * radius;

            Vector3 toCandidate = cand - target.position;
            if (toCandidate.sqrMagnitude > 0.0001f)
            {
                float dot = Vector3.Dot(target.forward.normalized, toCandidate.normalized);
                if (dot > 0.6f) continue;
            }

            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                if (Mathf.Abs(hit.position.y - owner.transform.position.y) > 6f) continue;
                cand = hit.position;
            }

            if (IsPositionFree(cand, crowdCheckRadius))
                return cand;
            candidates.Add(cand);
        }

        candidates.Sort((a, b) => CountNearbyAllies(a).CompareTo(CountNearbyAllies(b)));
        if (candidates.Count > 0) return candidates[0];

        return target.position + (owner.transform.position - target.position).normalized * radiusBase;
    }

    Vector3 PickFlankPosition(Transform enemy)
    {
        if (enemy == null) return owner.transform.position;
        Vector3 fromEnemy = (owner.transform.position - enemy.position);
        Vector3 flat = Vector3.ProjectOnPlane(fromEnemy, Vector3.up);
        if (flat.sqrMagnitude < 0.001f) flat = -owner.transform.forward;
        flat.Normalize();
        Vector3 perp = Vector3.Cross(Vector3.up, flat).normalized;
        float sideSign = Random.value < 0.5f ? 1f : -1f;
        float dist = Mathf.Clamp(owner.StrafeRadius * 1.6f, 6f, owner.ShootRange * 0.7f);
        Vector3 cand = enemy.position + flat * (dist * 0.6f) + perp * (sideSign * dist);
        if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            cand = hit.position;
        return FindFreeNearbyPoint(cand, 2.2f);
    }

    Vector3 FindFreeNearbyPoint(Vector3 center, float radius)
    {
        if (IsPositionFree(center, crowdCheckRadius)) return center;
        int attempts = 8;
        for (int i = 0; i < attempts; i++)
        {
            Vector3 cand = center + Random.insideUnitSphere * radius;
            cand.y = center.y;
            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                cand = hit.position;
            if (IsPositionFree(cand, crowdCheckRadius)) return cand;
        }
        return center;
    }

    bool IsPositionFree(Vector3 pos, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(pos, radius, LayerMask.GetMask("Tank"));
        if (hits == null || hits.Length == 0) return true;
        foreach (var c in hits)
        {
            if (c == null) continue;
            Transform t = c.transform;
            if (t.IsChildOf(owner.transform) || t == owner.transform) continue;
            TankAI otherAI = t.GetComponentInParent<TankAI>();
            if (otherAI == null) continue;
            if (otherAI == owner) continue;
            if (otherAI.teamComp != null && owner.teamComp != null && otherAI.teamComp.team == owner.teamComp.team) return false;
        }
        return true;
    }

    int CountNearbyAllies(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, crowdCheckRadius, LayerMask.GetMask("Tank"));
        int count = 0;
        if (hits == null) return 0;
        foreach (var c in hits)
        {
            if (c == null) continue;
            Transform t = c.transform;
            if (t.IsChildOf(owner.transform) || t == owner.transform) continue;
            TankAI otherAI = t.GetComponentInParent<TankAI>();
            if (otherAI == null) continue;
            if (otherAI.teamComp != null && owner.teamComp != null && otherAI.teamComp.team == owner.teamComp.team) count++;
        }
        return count;
    }

    void UpdateAgentDefaultMovement(Vector3 targetPosition)
    {
        if (owner.agent == null) return;
        float dist = Vector3.Distance(owner.transform.position, targetPosition);
        owner.agent.speed = owner.MoveSpeed;
        owner.agent.acceleration = Mathf.Max(1f, owner.MoveSpeed * 2f);
        owner.agent.angularSpeed = Mathf.Max(120f, owner.RotationSpeed * 30f);
        owner.agent.autoBraking = true;
        owner.agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        owner.agent.stoppingDistance = Mathf.Clamp(owner.ShootRange * 0.12f, 0.3f, 4f);
        if (dist < owner.ShootRange * 0.5f)
            owner.agent.speed = owner.MoveSpeed * 0.6f;
    }

    Transform DetermineTarget(out AIState nextState)
    {
        if (owner.currentTarget != null)
        {
            float distToEnemy = Vector3.Distance(owner.transform.position, owner.currentTarget.position);
            nextState = (distToEnemy <= owner.ShootRange) ? AIState.Fighting : AIState.Moving;
            return owner.currentTarget;
        }
        if (owner.currentCapturePointTarget != null)
        {
            nextState = AIState.Moving;
            return owner.currentCapturePointTarget.transform;
        }
        nextState = AIState.Patrolling;
        return null;
    }

    public void OnDrawGizmos()
    {
        if (!owner.debugGizmos) return;
        perception.DrawGizmos();
        navigation.DrawGizmos();

        if (hasTacticalPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentTacticalPosition, 0.5f);
            Gizmos.DrawLine(owner.transform.position, currentTacticalPosition);
        }
    }

    enum Tactic
    {
        None,
        Strafe,
        Flank,
        Retreat,
        Capture
    }
}
