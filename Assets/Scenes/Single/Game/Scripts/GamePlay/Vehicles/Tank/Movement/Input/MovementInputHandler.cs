using Serilog;
using UnityEngine;

public class MovementInputHandler
{
    readonly Tank owner;
    readonly MovementContext ctx;
    public MovementInputHandler(Tank owner, MovementContext ctx) { this.owner = owner; this.ctx = ctx; }
    public void OnEnable()
    {
        if (owner.actionsAsset == null)
        {
            Log.Error("[TankMovement] actionsAsset not assigned on {TankName}", owner.name);
            return;
        }
        ctx.moveAction = owner.actionsAsset.FindAction("Gameplay/Move", true);
        if (ctx.moveAction == null)
        {
            Log.Error("[TankMovement] Move action not found at path 'Gameplay/Move' on {TankName}", owner.name);
            return;
        }
        ctx.moveAction.Enable();

        if (owner.IdleSound != null) ctx.idleSource.Play();
        if (owner.DriveSound != null) ctx.driveSource.Play();
    }

    public void OnDisable()
    {
        ctx.moveAction?.Disable();
        if (ctx.idleSource != null) ctx.idleSource.Stop();
        if (ctx.driveSource != null) ctx.driveSource.Stop();
    }

    public void Update()
    {
        Vector2 v = ctx.moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        float rMove = Mathf.Clamp(v.y, -1f, 1f);
        float rTurn = Mathf.Clamp(v.x, -1f, 1f);

        ctx.rawMoveInput = Mathf.Abs(rMove) < owner.inputDeadzone ? 0f : rMove;
        ctx.rawTurnInput = Mathf.Abs(rTurn) < owner.inputDeadzone ? 0f : rTurn;

        ctx.rawMoveSmoothed = Mathf.Lerp(ctx.rawMoveSmoothed, rMove, 5f * Time.deltaTime);
        ctx.rawTurnSmoothed = Mathf.Lerp(ctx.rawTurnSmoothed, rTurn, 5f * Time.deltaTime);

        float absMove = Mathf.Abs(ctx.rawMoveSmoothed);
        float absTurn = Mathf.Abs(ctx.rawTurnSmoothed) * 0.5f;
        float targetBlend = Mathf.Clamp01(absMove + absTurn);
        ctx.currentBlend = Mathf.SmoothDamp(ctx.currentBlend, targetBlend, ref ctx.blendVelocity, 0.2f);

        float idleVolume = Mathf.Lerp(owner.MaxIdleVolume, owner.MinIdleVolume, ctx.currentBlend);
        float driveVolume = Mathf.Lerp(0f, owner.MaxDriveVolume, ctx.currentBlend);
        float idlePitch = Mathf.Lerp(owner.MaxIdlePitch, owner.MinIdlePitch, ctx.currentBlend);
        float drivePitch = Mathf.Lerp(owner.MinDrivePitch, owner.MaxDrivePitch, ctx.currentBlend);

        if (ctx.idleSource != null) { ctx.idleSource.volume = idleVolume; ctx.idleSource.pitch = idlePitch; }
        if (ctx.driveSource != null) { ctx.driveSource.volume = driveVolume; ctx.driveSource.pitch = drivePitch; }

        float currentSpeedMS = Vector3.Dot(owner.rb.linearVelocity, owner.transform.forward);
        float currentSpeedKmh = currentSpeedMS * 3.6f;
        if (owner.speedDisplay != null) owner.speedDisplay.SetSpeed((int)currentSpeedKmh);

        if (ctx.reverseLockTimer > 0f) ctx.reverseLockTimer -= Time.deltaTime;
    }
}
