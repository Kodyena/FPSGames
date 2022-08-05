using UnityEngine;

internal class PlayerJumpState : PlayerBaseState
{
    private float _jumpCooldown;

    public PlayerJumpState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory) { 
        IsRootState = true;
        InitializeSubState();
    }

    public override void CheckStateSwitch()
    {
        //Stop Jump if grounded or wall running start
        if (Context.IsGrounded && _jumpCooldown > .1f)
        {
            SwitchState(Factory.Grounded());
        }else if (Context.CanWallRun())
        {
            SwitchState(Factory.WallRun());
        }else if(Context.IsGrapplePressed && Context.GrappleHit.collider != null)
        {
            SwitchState(Factory.Grappling());
        }
        else if (Context.IsMeleePressed)
        {
            SwitchState(Factory.Melee());
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entering Jump State");
        Context.Animator.SetTrigger(Context.IsJumpingHash);
        _jumpCooldown = 0;
        Context.Rigidbody.velocity = new Vector3(Context.Rigidbody.velocity.x, 0f, Context.Rigidbody.velocity.z);
        float jumpForce = Mathf.Sqrt(Context.JumpHeight * -2 * Physics.gravity.y);
        Context.Rigidbody.AddForce((Vector3.up + Context.LastMovementDirection / 2) * jumpForce, ForceMode.Impulse);
        Context.Animator.SetBool(Context.IsJumpingHash, true);

    }

    public override void ExitState()
    {
        Debug.Log("Exiting Jump State.");

        Context.Animator.SetBool(Context.IsJumpingHash, false);
        Context.Animator.SetTrigger(Context.ResetStateHash);
    }

    public override void InitializeSubState()
    {
    }

    public override void AdditionalUpdateLogic()
    {
        _jumpCooldown += Time.deltaTime;
        Movement.BasicDirectionalMove(Context.Rigidbody, Context.LastMovementDirection, Context.CurrentSpeed * Context.AirControlFactor, Context.CurrentAcceleration);
    }
}