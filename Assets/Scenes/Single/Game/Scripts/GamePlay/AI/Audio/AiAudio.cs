using UnityEngine;

public class AIAudio
{
    readonly TankAI owner;
    public AIAudio(TankAI owner) { this.owner = owner; }

    public void UpdateEngineAudio()
    {
        if (owner.agent == null) return;

        float speed = owner.agent.velocity.magnitude;
        float blend = Mathf.Clamp01(speed / owner.MoveSpeed);

        if (owner.idleSource != null)
        {
            owner.idleSource.volume = Mathf.Lerp(owner.MaxIdleVolume, owner.MinIdleVolume, blend);
            owner.idleSource.pitch = Mathf.Lerp(owner.MaxIdlePitch, owner.MinIdlePitch, blend);
        }

        if (owner.driveSource != null)
        {
            owner.driveSource.volume = Mathf.Lerp(owner.MinDriveVolume, owner.MaxDriveVolume, blend);
            owner.driveSource.pitch = Mathf.Lerp(owner.MinDrivePitch, owner.MaxDrivePitch, blend);
        }
    }
}
