using UnityEngine;

public class MovementFixedHandler
{
    readonly TankMovement owner;
    readonly MovementContext ctx;
    readonly MovementPhysics physics;

    public MovementFixedHandler(TankMovement owner, MovementContext ctx, MovementPhysics physics) { this.owner = owner; this.ctx = ctx; this.physics = physics; }

    public void FixedUpdate()
    {
        if (Mathf.Abs(ctx.smoothedTurn) < 0.05f) ctx.smoothedTurn = 0f;

        ctx.smoothedMove = Mathf.SmoothDamp(ctx.smoothedMove, ctx.rawMoveInput, ref ctx.moveVelocity, 1f / Mathf.Max(owner.moveResponse, 0.1f));
        ctx.smoothedTurn = Mathf.SmoothDamp(ctx.smoothedTurn, ctx.rawTurnInput, ref ctx.turnVelocity, 1f / Mathf.Max(owner.turnResponse, 0.1f));

        float inputMagnitude = Mathf.Max(Mathf.Abs(ctx.smoothedMove), Mathf.Abs(ctx.smoothedTurn));
        float targetEnginePower = inputMagnitude > 0.01f ? 1f : 0f;
        ctx.enginePower = Mathf.MoveTowards(ctx.enginePower, targetEnginePower, Time.deltaTime * 0.8f);

        physics.HandleMovementPhysics(ctx.smoothedMove, ctx.smoothedTurn);
    }
}
