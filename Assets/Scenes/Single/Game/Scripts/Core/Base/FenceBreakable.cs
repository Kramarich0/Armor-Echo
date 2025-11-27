using UnityEngine;

[DisallowMultipleComponent]
public class BreakableObject : MonoBehaviour
{
    [Header("Physics Settings")]
    public float mass = 10f;
    public float linearDamping = 2f;
    public float angularDamping = 2f;

    [Header("Break Settings")]
    public float breakForce = 800f;
    public float breakTorque = 150f;
    public float minImpactToBreak = 10f;

    [Header("Optional Impulse on Break")]
    public float extraImpulse = 2f;
    public float randomImpulse = 1f;

    [Header("Parts (any object parts)")]
    public Transform[] parts;

    [Header("Audio")]
    public AudioClip breakSound;
    public float soundVolume = 1f;

    private bool isBroken = false;

    private void Awake()
    {
        if (parts == null || parts.Length == 0)
        {
            Debug.LogWarning("[BreakableObject] No parts assigned.");
            return;
        }

        foreach (var t in parts)
        {
            if (t == null) continue;

            if (!t.TryGetComponent<Collider>(out var col))
                t.gameObject.AddComponent<BoxCollider>();

            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (!rb) rb = t.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = mass;
            rb.linearDamping = linearDamping;
            rb.angularDamping = angularDamping;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (!t.TryGetComponent<BreakablePart>(out var helper))
                helper = t.gameObject.AddComponent<BreakablePart>();
            helper.owner = this;
        }
    }

    public void OnPartHit(float impact, Vector3 hitPoint, Collision collision)
    {
        if (isBroken) return;
        if (impact < minImpactToBreak) return;

        Break(hitPoint, collision);
    }

    private void Break(Vector3 hitPoint, Collision collision)
    {
        isBroken = true;

        Rigidbody[] rbs = new Rigidbody[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null) continue;
            Rigidbody rb = parts[i].GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rbs[i] = rb;
        }

        for (int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i] == null) continue;
            Vector3 dir = (parts[i].position - hitPoint).normalized;
            Vector3 impulse = dir * extraImpulse + Random.insideUnitSphere * randomImpulse;
            rbs[i].AddForce(impulse, ForceMode.Impulse);
        }

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, hitPoint, soundVolume);

        Debug.Log("[BreakableObject] Object broken!");
    }
}

[RequireComponent(typeof(Collider))]
public class BreakablePart : MonoBehaviour
{
    public BreakableObject owner;

    private void OnCollisionEnter(Collision collision)
    {
        if (owner == null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (collision.rigidbody != null)
            impact *= collision.rigidbody.mass;

        owner.OnPartHit(impact, collision.contacts[0].point, collision);
    }
}
