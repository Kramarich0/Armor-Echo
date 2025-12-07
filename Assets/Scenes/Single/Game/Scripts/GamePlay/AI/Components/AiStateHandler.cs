// AIStateHandler.cs  (обновлённая версия)
// Внесены изменения: тактический выбор позиций, смена решений по таймеру,
// anti-crowd при выборе позиции, микро-движения, стрельба при движении.
// Все правки помечены "// NEW"

using Serilog;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AIStateHandler
{
    readonly TankAI owner;
    readonly AIPerception perception;
    readonly AINavigation navigation;
    readonly AICombat combat;
    readonly AIWeapons weapons;

    // NEW: decision timer + tactical state
    float decisionTimer = 0f;                        // время до следующего тактического решения
    float minDecisionInterval = 1.5f;                // NEW: min interval between decisions
    float maxDecisionInterval = 3.0f;                // NEW: max interval between decisions

    Vector3 currentTacticalPosition;                 // NEW: цель перемещения (battle position / strafe point)
    bool hasTacticalPosition = false;                // NEW
    Tactic currentTactic = Tactic.None;              // NEW

    // NEW: anti-crowd / radius to check for other tanks when picking spot
    const float crowdCheckRadius = 3.0f;

    // NEW: micro-movement amplitude (small jitter while holding position)
    float microMoveTimer = 0f;
    const float microMoveInterval = 0.6f;
    const float microMoveRadius = 0.6f;

    public AIStateHandler(TankAI owner, AIPerception perception, AINavigation navigation, AICombat combat, AIWeapons weapons)
    {
        this.owner = owner;
        this.perception = perception;
        this.navigation = navigation;
        this.combat = combat;
        this.weapons = weapons;

        decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval); // NEW: start random timer
    }

    public void UpdateState()
    {
        // perception always updates first
        perception.UpdatePerception();

        // NEW: update strafing phase with slight randomization so bots don't sync perfectly
        owner.strafePhase = (owner.strafePhase + Time.deltaTime * owner.StrafeSpeed * (1f + (Mathf.Sin(Time.time * 0.3f) * 0.05f))) % 1f;

        // NEW: countdown decision timer
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            MakeTacticalDecision(); // choose tactic/position
            decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval);
        }

        // Update target & state
        Transform perceivedTarget = DetermineTarget(out AIState nextState);
        owner.currentState = nextState;

        // If there is an immediate enemy in range, prefer it
        Transform engageTarget = perceivedTarget;

        if (nextState == AIState.Fighting && engageTarget != null)
        {
            // Aim continuously (can rotate turret while moving)
            combat.AimAt(engageTarget);

            // Shooting: check LOS and angle, shoot while moving
            TryShootAt(engageTarget);

            // Movement: perform current tactic
            PerformTacticBehavior(engageTarget);
        }
        else if (engageTarget != null)
        {
            // No immediate fight (target far) — move towards it
            // Reset tactical position so decision will re-evaluate when close
            hasTacticalPosition = false;
            navigation.MoveTo(engageTarget.position);
            UpdateAgentDefaultMovement(engageTarget.position);
        }
        else
        {
            // No targets — if we have capture point, move to it; else patrol
            if (owner.currentCapturePointTarget != null)
            {
                // If an enemy appears while heading to capture point, perception will catch it and decision logic will switch
                hasTacticalPosition = false;
                navigation.MoveTo(owner.currentCapturePointTarget.transform.position);
            }
            else
            {
                navigation.PatrolRandom();
            }
        }

        // NEW: micro-movements when holding position (prevents static stacking)
        if (hasTacticalPosition && Vector3.Distance(owner.transform.position, currentTacticalPosition) < Mathf.Max(1f, owner.StrafeRadius * 0.3f))
        {
            microMoveTimer -= Time.deltaTime;
            if (microMoveTimer <= 0f)
            {
                microMoveTimer = microMoveInterval * Random.Range(0.8f, 1.4f);
                Vector3 jitter = Random.insideUnitSphere * microMoveRadius;
                jitter.y = 0f;
                Vector3 microTarget = currentTacticalPosition + jitter;
                navigation.MoveTo(microTarget); // small move to appear alive
            }
        }
    }

    // NEW: attempt to shoot with checks; allows shooting while moving
    void TryShootAt(Transform target)
    {
        if (Time.time < owner.nextFireTime) return;

        if (owner.gunEnd == null)
        {
            if (owner.debugLogs) Log.Warning("[AI] GunEnd is null");
            return;
        }

        bool los = perception.HasLineOfSight(target);
        Vector3 aimDir = (target.position - owner.gunEnd.position).normalized;
        float angle = Vector3.Angle(owner.gunEnd.forward, aimDir);
        float shootAngleThreshold = owner.enableStrafeWhileShooting ? 28f : 22f; // NEW: slightly relaxed threshold

        if (owner.debugLogs)
            Log.Debug("[AI] TryShoot: LOS={LOS} angle={Angle} threshold={Threshold}", los, angle, shootAngleThreshold);

        if (los && angle < shootAngleThreshold)
        {
            weapons.ShootAt(target);
            owner.nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, owner.FireRate);
        }
    }

    // NEW: central method that executes movement according to chosen tactic
    void PerformTacticBehavior(Transform target)
    {
        // If no tactical pos or it's invalid, re-decide next frame
        if (!hasTacticalPosition)
        {
            // fallback to a strafe point immediately if no tactical pos
            currentTacticalPosition = PickBattlePositionNearTarget(target);
            hasTacticalPosition = true;
            currentTactic = Tactic.Strafe;
        }

        // If tactic is to capture, behave differently (but still engage enemies)
        switch (currentTactic)
        {
            case Tactic.Strafe:
                {
                    // Move to tactical point (strafe position) but keep distance safety
                    if (Vector3.Distance(owner.transform.position, currentTacticalPosition) < Mathf.Max(1f, owner.StrafeRadius * 0.4f))
                    {
                        // hold position (micro-movement will run)
                        // ensure agent stopping distance small for aiming while moving
                        if (owner.agent != null) owner.agent.stoppingDistance = Mathf.Max(0.3f, owner.ShootRange * 0.05f);
                    }
                    else
                    {
                        navigation.MoveTo(currentTacticalPosition);
                    }
                    break;
                }
            case Tactic.Flank:
                {
                    // Flank — we move to flank position and then possibly rush if favorable
                    navigation.MoveTo(currentTacticalPosition);
                    if (owner.agent != null) owner.agent.stoppingDistance = Mathf.Clamp(owner.ShootRange * 0.12f, 0.3f, owner.ShootRange * 0.4f);
                    break;
                }
            case Tactic.Retreat:
                {
                    // Retreat/backoff — keep moving backwards relative to enemy
                    navigation.MoveTo(currentTacticalPosition);
                    if (owner.agent != null) owner.agent.stoppingDistance = Mathf.Max(0.3f, owner.ShootRange * 0.05f);
                    break;
                }
            case Tactic.Capture:
                {
                    // Move to capture point but allow shooting when enemy in range
                    if (owner.currentCapturePointTarget != null)
                        navigation.MoveTo(owner.currentCapturePointTarget.transform.position);
                    break;
                }
            default:
                {
                    // fallback: move to target but keep safe distance
                    float dist = Vector3.Distance(owner.transform.position, target.position);
                    if (dist < owner.ShootRange * 0.6f)
                        navigation.MoveTo(owner.transform.position + (owner.transform.position - target.position).normalized * (owner.ShootRange * 0.6f));
                    else
                        navigation.MoveTo(target.position);
                    break;
                }
        }
    }

    // NEW: main tactical decision-maker chosen periodically
    void MakeTacticalDecision()
    {
        // If no target, nothing much to decide (patrol/capture handled in UpdateState)
        Transform enemy = owner.currentTarget;
        Transform cap = owner.currentCapturePointTarget != null ? owner.currentCapturePointTarget.transform : null;

        // If enemy exists and in relatively close range, choose combat tactics
        if (enemy != null)
        {
            float dist = Vector3.Distance(owner.transform.position, enemy.position);

            // If enemy very close -> retreat (backoff) with some chance
            if (dist < owner.ShootRange * 0.35f && Random.value < 0.7f)
            {
                // NEW: retreat position = back off along vector from enemy and try to keep free spot
                Vector3 back = owner.transform.position + (owner.transform.position - enemy.position).normalized * Mathf.Clamp(owner.StrafeRadius * 1.1f, 4f, owner.ShootRange * 0.6f);
                currentTacticalPosition = FindFreeNearbyPoint(back, owner.StrafeRadius * 0.6f);
                currentTactic = Tactic.Retreat;
                hasTacticalPosition = true;
                return;
            }

            // If has allies clustered around target, try to flank
            if (IsAllyClusteredNear(enemy.position, 4f) && Random.value < 0.6f)
            {
                currentTacticalPosition = PickFlankPosition(enemy);
                currentTactic = Tactic.Flank;
                hasTacticalPosition = true;
                return;
            }

            // General combat: pick strafe point around enemy
            currentTacticalPosition = PickBattlePositionNearTarget(enemy);
            currentTactic = Tactic.Strafe;
            hasTacticalPosition = true;
            return;
        }

        // If no enemy but capture point present, go capture but stay alert
        if (cap != null)
        {
            // If enemy appears close by soon, we will switch next tick — but default is capture
            currentTactic = Tactic.Capture;
            hasTacticalPosition = false; // let MoveTo handle capture point
            return;
        }

        // Default: patrol/random move
        currentTactic = Tactic.None;
        hasTacticalPosition = false;
    }

    // NEW: pick a battle/strafe position that avoids allies and obstacles
    Vector3 PickBattlePositionNearTarget(Transform target)
    {
        if (target == null) return owner.transform.position;

        // try several candidate offsets around target, prefer those not crowded
        int attempts = 8;
        float radiusBase = Mathf.Max(2f, owner.StrafeRadius);
        List<Vector3> candidates = new List<Vector3>(attempts);

        for (int i = 0; i < attempts; i++)
        {
            float angle = (i / (float)attempts) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float radius = radiusBase * Random.Range(0.85f, 1.25f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 cand = target.position + dir * radius;

            // project to navmesh
            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                cand = hit.position;

            if (IsPositionFree(cand, crowdCheckRadius))
                return cand;
            candidates.Add(cand);
        }

        // fallback: return nearest candidate (least crowded)
        candidates.Sort((a, b) => CountNearbyAllies(a).CompareTo(CountNearbyAllies(b)));
        return candidates.Count > 0 ? candidates[0] : target.position + (owner.transform.position - target.position).normalized * radiusBase;
    }

    // NEW: pick a flank position (further out and off to side)
    Vector3 PickFlankPosition(Transform enemy)
    {
        if (enemy == null) return owner.transform.position;
        Vector3 toEnemy = owner.transform.position - enemy.position;
        Vector3 flat = Vector3.ProjectOnPlane(toEnemy, Vector3.up);
        if (flat.sqrMagnitude < 0.001f) flat = -owner.transform.forward;
        flat.Normalize();
        Vector3 perp = Vector3.Cross(flat, Vector3.up).normalized;

        // choose left or right flank randomly
        float side = Random.value < 0.5f ? 1f : -1f;
        float dist = Mathf.Clamp(owner.StrafeRadius * 1.4f, 6f, owner.ShootRange * 0.6f);
        Vector3 cand = enemy.position + (flat * dist * 0.6f) + (perp * side * dist);

        if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            cand = hit.position;

        return FindFreeNearbyPoint(cand, 2.0f);
    }

    // NEW: try to find a nearby free point (avoid allies)
    Vector3 FindFreeNearbyPoint(Vector3 center, float radius)
    {
        if (IsPositionFree(center, crowdCheckRadius)) return center;

        int attempts = 6;
        for (int i = 0; i < attempts; i++)
        {
            Vector3 cand = center + Random.insideUnitSphere * radius;
            cand.y = center.y;
            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                cand = hit.position;
            if (IsPositionFree(cand, crowdCheckRadius)) return cand;
        }
        return center; // fallback
    }

    // NEW: simple check if other allies are too close
    bool IsPositionFree(Vector3 pos, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(pos, radius, LayerMask.GetMask("Tank"));
        if (hits == null || hits.Length == 0) return true;

        // allow self presence
        foreach (var c in hits)
        {
            if (c.transform.IsChildOf(owner.transform) || c.transform == owner.transform)
                continue;
            // if other tank is alive and not ourselves -> occupied
            return false;
        }
        return true;
    }

    // NEW: count allies around pos (for picking least-crowded candidate)
    int CountNearbyAllies(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, crowdCheckRadius, LayerMask.GetMask("Tank"));
        int count = 0;
        if (hits == null) return 0;
        foreach (var c in hits)
        {
            if (c.transform.IsChildOf(owner.transform) || c.transform == owner.transform) continue;
            count++;
        }
        return count;
    }

    // NEW: detect if allies clustered near a point
    bool IsAllyClusteredNear(Vector3 pos, float radius)
    {
        return CountNearbyAllies(pos) >= 2;
    }

    // Try to keep agent movement params sane (shared utility)
    void UpdateAgentDefaultMovement(Vector3 targetPosition)
    {
        if (owner.agent == null) return;
        float dist = Vector3.Distance(owner.transform.position, targetPosition);
        owner.agent.stoppingDistance = Mathf.Clamp(owner.ShootRange * 0.12f, 0.3f, owner.ShootRange * 0.4f);
        owner.agent.speed = (dist < owner.ShootRange * 0.45f) ? owner.MoveSpeed * 0.45f : owner.MoveSpeed;
    }

    Transform DetermineTarget(out AIState nextState)
    {
        // NOTE: Keep enemy preference — if enemy inside shoot range, fight; otherwise move to capture if closer
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

        // NEW: show tactical point
        if (hasTacticalPosition)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentTacticalPosition, 0.5f);
            Gizmos.DrawLine(owner.transform.position, currentTacticalPosition);
        }
    }

    Vector3 GetStrafePoint(Transform target, float radius, float speed, float phaseOffset)
    {
        // Kept for compatibility but not used when tactical positions are chosen by MakeTacticalDecision.
        if (target == null) return owner.transform.position;

        float phase = (Time.time * Mathf.Max(0.001f, speed) + phaseOffset + Random.value * 0.2f) % 1f; // NEW: small randomization
        float angle = phase * Mathf.PI * 2f;

        Vector3 toBot = owner.transform.position - target.position;
        Vector3 flat = Vector3.ProjectOnPlane(toBot, Vector3.up);
        if (flat.sqrMagnitude < 0.001f) flat = -owner.transform.forward;
        flat.Normalize();
        Vector3 perp = Vector3.Cross(flat, Vector3.up).normalized;

        float rad = radius * Random.Range(0.9f, 1.1f); // NEW: slight radius variation
        Vector3 offset = (perp * Mathf.Cos(angle) + flat * Mathf.Sin(angle)) * rad;
        return target.position + offset;
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
