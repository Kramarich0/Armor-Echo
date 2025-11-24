using System.Collections;
using System.Collections.Generic;
using Serilog;
using UnityEngine;

[RequireComponent(typeof(TeamComponent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(IDamageable))]
public class TankCollisionDamage : MonoBehaviour
{
    [Header("Collision damage settings")]
    public float minCollisionSpeed = 3f;
    [Tooltip("Multiplier to tune damage scale. Start with 0.01 - 0.1 and tune.")]
    public float damageMultiplier = 0.02f;
    public bool debugLogs = false;

    private readonly HashSet<int> processedCollisionIds = new();
    private readonly Queue<(int id, float removeTime)> collisionQueue = new();

    private Rigidbody rb;
    internal TeamComponent teamComp;
    private IDamageable selfDamageable;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        teamComp = GetComponent<TeamComponent>();
        selfDamageable = GetComponent<IDamageable>();

        if (rb == null)
            Log.Error("[TankCollisionDamage] Missing component: Rigidbody on {TankName}", name);

        if (teamComp == null)
            Log.Error("[TankCollisionDamage] Missing component: TeamComponent on {TankName}", name);

        if (selfDamageable == null)
            Log.Error("[TankCollisionDamage] Missing component: TankHealth on {TankName}", name);
    }

    void FixedUpdate()
    {
        float now = Time.time;
        while (collisionQueue.Count > 0 && collisionQueue.Peek().removeTime <= now)
        {
            var (id, _) = collisionQueue.Dequeue();
            processedCollisionIds.Remove(id);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.rigidbody == null) { if (debugLogs) Log.Debug("[TC] collision.rigidbody == null"); ; return; }

        int otherId = collision.collider.GetInstanceID();
        if (processedCollisionIds.Contains(otherId)) return;

        processedCollisionIds.Add(otherId);
        collisionQueue.Enqueue((otherId, Time.time + Time.fixedDeltaTime));

        IDamageable otherDamageable = collision.collider.GetComponentInParent<IDamageable>();
        TeamComponent otherTeam = collision.collider.GetComponentInParent<TeamComponent>();

        if (otherDamageable == null) { if (debugLogs) Log.Debug("[TC] other has no IDamageable"); return; }
        if (otherTeam != null && otherTeam.team == teamComp.team) { if (debugLogs) Log.Debug("[TC] collision with teammate - ignored"); return; }

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null) { if (debugLogs) Log.Debug("[TC] other has no Rigidbody"); ; return; }

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minCollisionSpeed * minCollisionSpeed) { if (debugLogs) Log.Debug("[TC] impactSpeed {ImpactSpeed} < min {Min}", Mathf.Sqrt(impactSpeed), minCollisionSpeed); return; }

        float massThis = rb.mass;
        float massOther = otherRb.mass;

        int damageToOther = Mathf.Max(1,
            Mathf.RoundToInt(massThis * impactSpeed * impactSpeed * damageMultiplier));

        int damageToThis = Mathf.Max(1,
            Mathf.RoundToInt(massOther * impactSpeed * impactSpeed * damageMultiplier));

        if (debugLogs)
        {
            Log.Debug("[TC] Collide: {ThisName} (mass={MassThis}) <-> {OtherName} (mass={MassOther}) | v={ImpactSpeed} => dmgToOther={DamageToOther}, dmgToThis={DamageToThis}",
                      name, massThis, collision.collider.name, massOther, impactSpeed, damageToOther, damageToThis);
        }

        otherDamageable.TakeDamage(damageToOther, source: gameObject.name);
        selfDamageable.TakeDamage(damageToThis, source: collision.gameObject.name);
    }

}
