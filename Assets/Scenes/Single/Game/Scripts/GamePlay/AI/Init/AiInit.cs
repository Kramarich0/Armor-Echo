using System.Collections;
using Serilog;
using UnityEngine;
using UnityEngine.AI;

public class AIInit
{
    readonly TankAI owner;
    public AIInit(TankAI owner) { this.owner = owner; }


    public void Awake()
    {
        owner.tankHealth = owner.GetComponent<AITankHealth>();
        owner.agent = owner.GetComponent<NavMeshAgent>();
        owner.teamComp = owner.GetComponent<TeamComponent>();
    }


    public void Start()
    {

        if (owner.agent != null)
        {
            owner.agent.speed = owner.MoveSpeed;
            owner.agent.angularSpeed = 120f;
            owner.agent.acceleration = 8f;
            owner.agent.updateRotation = false;
            owner.agent.updatePosition = false;
            owner.agent.stoppingDistance = 0f;
            owner.agent.autoBraking = false;
            owner.agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            owner.strafePhase = Random.value;

            if (NavMesh.SamplePosition(owner.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                owner.agent.Warp(hit.position);
                owner.navAvailable = true;

                if (owner.debugLogs)
                    Log.Debug("[AI] NavMesh found and agent warped at {WarpPosition}", hit.position);
            }
            else
            {
                owner.navAvailable = false;

                if (owner.debugLogs)
                    Log.Warning("[AI] NavMesh not found nearby for {AgentName}. Using fallback movement.", owner.name);
            }

        }

        if (owner.leftTrack != null && owner.rightTrack != null)
        {
            if (owner.TryGetComponent<Rigidbody>(out var rb))
                TankWheelSetup.ApplyToAllWheels(owner.leftTrack.wheels, owner.rightTrack.wheels, rb.mass);
        }

        owner.StartCoroutine(DelayedSetupAudio());
    }

    IEnumerator DelayedSetupAudio()
    {

        const int maxFrames = 10;
        int frames = 0;
        while (AudioManager.Instance == null && frames < maxFrames)
        {
            frames++;
            yield return null;
        }


        SetupAudio();
    }

    void SetupAudio()
    {
        if (owner.IdleSound != null)
        {
            owner.idleSource = owner.gameObject.AddComponent<AudioSource>();
            AudioManager.AssignToMaster(owner.idleSource);
            owner.idleSource.clip = owner.IdleSound;
            owner.idleSource.loop = true;
            owner.idleSource.spatialBlend = 1f;
            owner.idleSource.minDistance = 3f;
            owner.idleSource.maxDistance = 50f;
            owner.idleSource.rolloffMode = AudioRolloffMode.Logarithmic;
            owner.idleSource.volume = owner.MinIdlePitch;
            owner.idleSource.Play();
        }

        if (owner.DriveSound != null)
        {
            owner.driveSource = owner.gameObject.AddComponent<AudioSource>();
            AudioManager.AssignToMaster(owner.driveSource);
            owner.driveSource.clip = owner.DriveSound;
            owner.driveSource.loop = true;
            owner.driveSource.spatialBlend = 1f;
            owner.driveSource.minDistance = 3f;
            owner.driveSource.maxDistance = 50f;
            owner.driveSource.rolloffMode = AudioRolloffMode.Logarithmic;
            owner.driveSource.volume = owner.MinDriveVolume;
            owner.driveSource.Play();
        }

        if (owner.ShootSound != null)
        {
            owner.shootSource = owner.gameObject.AddComponent<AudioSource>();
            AudioManager.AssignToMaster(owner.shootSource);
            owner.shootSource.clip = owner.ShootSound;
            owner.shootSource.loop = false;
            owner.shootSource.spatialBlend = 1f;
            owner.shootSource.minDistance = 3f;
            owner.shootSource.maxDistance = 50f;
            owner.shootSource.rolloffMode = AudioRolloffMode.Logarithmic;
            owner.shootSource.volume = 0.6f;
        }
    }
}
