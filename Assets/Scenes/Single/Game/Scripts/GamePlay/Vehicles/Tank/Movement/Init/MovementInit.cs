using Serilog;
using UnityEngine;

public class MovementInit
{
    readonly Tank owner;
    readonly MovementContext ctx;

    public MovementInit(Tank owner, MovementContext ctx) { this.owner = owner; this.ctx = ctx; }

    public void Awake()
    {
        if (owner.rb == null) owner.rb = owner.GetComponent<Rigidbody>();
        if (owner.rb == null)
        {
            Log.Error("[TankMovement] Rigidbody not found on {TankName}", owner.name);
            return;
        }

        if (owner.enforceMinimumMass && owner.rb.mass < owner.minRecommendedMass)
        {
            Log.Warning("[TankMovement] Rigidbody mass ({RbMass}) below recommended ({RecommendedMass}). Setting to recommended.",
                        owner.rb.mass, owner.minRecommendedMass);
            owner.rb.mass = owner.minRecommendedMass;
        }

        owner.rb.isKinematic = false;
        owner.rb.useGravity = true;
        owner.rb.interpolation = RigidbodyInterpolation.Interpolate;
        owner.rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (owner.rb.angularDamping < 0.5f) owner.rb.angularDamping = 0.5f;

        ctx.idleSource = owner.gameObject.AddComponent<AudioSource>();
        ctx.driveSource = owner.gameObject.AddComponent<AudioSource>();
        AudioManager.AssignToMaster(ctx.idleSource);
        AudioManager.AssignToMaster(ctx.driveSource);
        SetupAudioSource(ctx.idleSource, owner.IdleSound, true);
        SetupAudioSource(ctx.driveSource, owner.DriveSound, true);

        if (owner.leftTrack != null && owner.rightTrack != null)
            TankWheelSetup.ApplyToAllWheels(owner.leftTrack.wheels, owner.rightTrack.wheels, owner.rb.mass);
    }

    void SetupAudioSource(AudioSource source, AudioClip clip, bool loop)
    {
        if (source == null) return;
        if (clip != null)
        {
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = 0f;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 5f;
            source.maxDistance = 500f;
        }
    }
}
