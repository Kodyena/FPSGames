using System.Diagnostics;
using Debug = UnityEngine.Debug;

internal class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory){}

    public override void CheckStateSwitch()
    {
        //Can switch to Run, Walk or Crouch
        if (Context.IsCrouchPressed)
        {
            SwitchState(Factory.Crouch());
        }else if(Context.IsMovementPressed & !Context.IsRunPressed)
        {
            SwitchState(Factory.Walk());
        }else if(Context.IsMovementPressed && Context.IsRunPressed)
        {
            SwitchState(Factory.Run());
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entered Idle State.");
        Context.Animator.SetTrigger(Context.ResetStateHash);
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Idle State.");
    }

    public override void InitializeSubState()
    {
        //no substate
    }

    public override void AdditionalUpdateLogic()
    {
    }
}