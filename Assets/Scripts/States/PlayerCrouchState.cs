using DG.Tweening;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

internal class PlayerCrouchState : PlayerBaseState
{
    private PlayerStateMachine context;
    private PlayerStateFactory playerStateFactory;

    public PlayerCrouchState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory) { }

    public override void CheckStateSwitch()
    {
        if (!Context.CrouchObstructedAbove())
        {
            if (!Context.IsCrouchPressed && !Context.IsMovementPressed && !Context.IsRunPressed)
            {
                SwitchState(Factory.Idle());
            }
            else if (!Context.IsCrouchPressed && !Context.IsRunPressed && Context.IsMovementPressed)
            {
                SwitchState(Factory.Walk());
            }
            else if (!Context.IsCrouchPressed && Context.IsRunPressed)
            {
                SwitchState(Factory.Run());
            }
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entered Crouch State.");
        Context.CurrentSpeed = Context.CrouchSpeed;
        Context.CurrentAcceleration = Context.CrouchAcceleration;
        Context.Collider.height = Context.CrouchHeight;
        Context.CurrentCMCamera.transform.localPosition = new UnityEngine.Vector3(0, Context.CurrentCMCamera.transform.localPosition.y * Context.CrouchHeight / Context.StandingHeight, 0);
        Context.Rigidbody.AddForce(Vector3.down * 10);

    }


    public override void ExitState()
    {
        Debug.Log("Exited Crouch State.");
        Context.transform.position += Vector3.up * (Context.StandingHeight - Context.CrouchHeight) / 2;
        Context.Collider.height = Context.StandingHeight;
        Context.CurrentCMCamera.transform.localPosition = new UnityEngine.Vector3(0, Context.CurrentCMCamera.transform.localPosition.y * Context.StandingHeight / Context.CrouchHeight, 0);
    }

    public override void InitializeSubState()
    {
    }

    public override void AdditionalUpdateLogic()
    {
        Movement.BasicDirectionalMove(Context.Rigidbody, Context.LastMovementDirection, Context.CurrentSpeed, Context.CurrentAcceleration);
    }
}