using DG.Tweening;
using System.Collections;
using UnityEngine;

class PlayerSlidingState : PlayerBaseState
{

    private float _currentSlidingTime;
    private Vector3 _slideStartVelocity;

    public PlayerSlidingState(PlayerStateMachine context, PlayerStateFactory factory) : base(context, factory)
    {
    }

    public override void CheckStateSwitch()
    {
        //Stop sliding if time run out or not holding crouch
        if (!Context.IsCrouchPressed || _currentSlidingTime > Context.MaxSlideTime)
        {
            SwitchState(Factory.Grounded());
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entered Sliding State.");
        Context.Animator.SetBool(Context.IsSlidingHash, true);
        Context.CurrentSpeed = Context.SlideSpeed;
        Context.Collider.height = Context.CrouchHeight;
        Context.CurrentCMCamera.transform.localPosition = new UnityEngine.Vector3(0, Context.CurrentCMCamera.transform.localPosition.y * Context.CrouchHeight / Context.StandingHeight, 0);
        _currentSlidingTime = 0;
        _slideStartVelocity = Context.Rigidbody.velocity;
    }

    public override void ExitState()
    {
        Debug.Log("Exited Sliding State.");
        Context.Animator.SetBool(Context.IsSlidingHash, false);
        Context.Collider.height = Context.StandingHeight;
        Context.CurrentCMCamera.transform.localPosition = new UnityEngine.Vector3(0, Context.CurrentCMCamera.transform.localPosition.y * Context.StandingHeight / Context.CrouchHeight, 0);
    }

    public override void InitializeSubState()
    {
    }

    public override void AdditionalUpdateLogic()
    {
        _currentSlidingTime += Time.deltaTime;
        Movement.Slide(Context.Rigidbody, Context.SlideDecayCurve, _slideStartVelocity, _currentSlidingTime, Context.MaxSlideTime);
    }
}