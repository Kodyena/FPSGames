using System.Diagnostics;
using System.Linq.Expressions;
using Debug = UnityEngine.Debug;

internal class PlayerFallingState : PlayerBaseState
{
    public PlayerFallingState(PlayerStateMachine context, PlayerStateFactory factory) : base(context, factory)
    {
        IsRootState = true;
    }

    public override void CheckStateSwitch()
    {
        if (Context.IsGrounded)
        {
            SwitchState(Factory.Grounded());
        }
        else if (Context.IsGrapplePressed && Context.GrappleHit.collider != null)
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
        Debug.Log("Entered falling state.");
        Context.Animator.SetBool(Context.IsFallingHash, true);
    }

    public override void ExitState()
    {
        Debug.Log("Exiting falling state.");
        Context.Animator.SetBool(Context.ResetStateHash, false);
        Context.Animator.SetTrigger(Context.ResetStateHash);
    }

    public override void InitializeSubState()
    {
        
    }

    public override void AdditionalUpdateLogic()
    {
        Movement.BasicDirectionalMove(Context.Rigidbody, Context.LastMovementDirection, Context.CurrentSpeed * Context.AirControlFactor, Context.CurrentAcceleration);
    }
}