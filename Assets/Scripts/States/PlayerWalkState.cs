using Debug = UnityEngine.Debug;

internal class PlayerWalkState : PlayerBaseState
{
    private PlayerStateMachine context;
    private PlayerStateFactory playerStateFactory;

    public PlayerWalkState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory) { }

    public override void CheckStateSwitch()
    {
        if (Context.IsCrouchPressed)
        {
            SwitchState(Factory.Crouch());
        }else if(!Context.IsMovementPressed && !Context.IsRunPressed)
        {
            SwitchState(Factory.Idle());
        }else if (Context.IsRunPressed)
        {
            SwitchState(Factory.Run());
        }
    }

    public override void AdditionalUpdateLogic()
    {
        Movement.BasicDirectionalMove(Context.Rigidbody, Context.LastMovementDirection, Context.CurrentSpeed, Context.CurrentAcceleration);
    }

    public override void EnterState()
    {
        Debug.Log("Entered Walking State.");
        Context.Animator.SetBool(Context.IsWalkingHash, true);
        Context.CurrentSpeed = Context.WalkingSpeed;
        Context.CurrentAcceleration = Context.WalkingAcceleration;
    }

    public override void ExitState()
    {
        Debug.Log("Exited Walking State.");
        Context.Animator.SetBool(Context.IsWalkingHash, false);
        Context.Animator.SetTrigger(Context.ResetStateHash);
    }

    public override void InitializeSubState()
    {
        throw new System.NotImplementedException();
    }
}