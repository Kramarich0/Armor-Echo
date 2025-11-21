using UnityEngine;
using UnityEngine.InputSystem;

public class MovementContext
{
    public InputAction moveAction;

    public float currentBlend = 0f;
    public float blendVelocity = 0f;
    public float rawMoveInput = 0f;
    public float rawTurnInput = 0f;
    public float smoothedMove = 0f;
    public float smoothedTurn = 0f;
    public float enginePower = 0f;
    public float rawMoveSmoothed = 0f;
    public float rawTurnSmoothed = 0f;
    public float moveVelocity = 0f;
    public float turnVelocity = 0f;

    public AudioSource idleSource;
    public AudioSource driveSource;
    public float reverseLockTimer = 0f;
}
