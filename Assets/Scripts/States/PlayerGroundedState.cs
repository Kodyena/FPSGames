using UnityEngine;

class PlayerGroundedState : PlayerBaseState
{
    public PlayerGroundedState(PlayerStateMachine context, PlayerStateFactory factory) : base(context, factory)
    {
        IsRootState = true;
        InitializeSubState();
    }

    public override void CheckStateSwitch()
    {
        if (Context.IsJumpPressed && Context.JumpEnabled)
        {
            SwitchState(Factory.Jump());
        }else if (!Context.IsGrounded)
        {
            SwitchState(Factory.Falling());
        }
        else if (Context.IsGrapplePressed && Context.GrappleHit.collider != null)
        {
            SwitchState(Factory.Grappling());
        }else if (Context.IsMeleePressed)
        {
            SwitchState(Factory.Melee());
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entered grounded state.");
    }

    public override void ExitState()
    {
        Debug.Log("Exited grounded state.");
    }

    public override void InitializeSubState()
    {
        if(!Context.IsMovementPressed && !Context.IsRunPressed)
        {
            SetSubState(Factory.Idle());
        }else if(Context.IsMovementPressed && !Context.IsRunPressed)
        {
            SetSubState(Factory.Walk());
        }else {
            SetSubState(Factory.Run());
        }
    }

    public override void AdditionalUpdateLogic()
    {
    }
}