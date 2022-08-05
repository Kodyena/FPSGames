using UnityEngine;

internal class PlayerRunState : PlayerBaseState
{
    private PlayerStateMachine context;
    private PlayerStateFactory playerStateFactory;

    public PlayerRunState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory) { }

    public override void CheckStateSwitch()
    {
        if (Context.IsCrouchPressed)
        {
            SwitchState(Factory.Sliding());
        }else if(!Context.IsRunPressed && !Context.IsMovementPressed)
        {
            SwitchState(Factory.Idle());
        }else if(!Context.IsRunPressed && Context.IsMovementPressed)
        {
            SwitchState(Factory.Walk());
        }
    }

    public override void AdditionalUpdateLogic()
    {
        Movement.BasicDirectionalMove(Context.Rigidbody, Context.LastMovementDirection, Context.CurrentSpeed, Context.CurrentAcceleration);
    }

    public override void EnterState()
    {
        Debug.Log("Entered Run State.");
        Context.Animator.SetBool(Context.IsRunningHash, true);
        Context.CurrentSpeed = Context.RunningSpeed;
        Context.CurrentAcceleration = Context.RunningAcceleration;
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Run State.");
        Context.Animator.SetBool(Context.IsRunningHash, false);
        Context.Animator.SetTrigger(Context.ResetStateHash);
    }

    public override void InitializeSubState()
    {
        throw new System.NotImplementedException();
    }
}