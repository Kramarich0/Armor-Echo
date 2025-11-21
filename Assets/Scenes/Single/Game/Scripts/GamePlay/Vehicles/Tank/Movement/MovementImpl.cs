public class TankMovementImpl
{
    private readonly TankMovement owner;
    readonly MovementContext ctx;
    readonly MovementInit init;
    readonly MovementInputHandler input;
    readonly MovementPhysics physics;
    readonly MovementFixedHandler fixedHandler;

    public TankMovementImpl(TankMovement owner)
    {
        this.owner = owner;
        ctx = new MovementContext();
        init = new MovementInit(owner, ctx);
        physics = new MovementPhysics(owner, ctx);
        input = new MovementInputHandler(owner, ctx);
        fixedHandler = new MovementFixedHandler(owner, ctx, physics);
    }

    public void Awake() => init.Awake();
    public void OnEnable() => input.OnEnable();
    public void OnDisable() => input.OnDisable();
    public void Update() => input.Update();
    public void FixedUpdate() => fixedHandler.FixedUpdate();
}
