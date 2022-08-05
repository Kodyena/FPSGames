using DG.Tweening;
using UnityEngine;

public class PlayerGrappleState : PlayerBaseState
{
    private SpringJoint _joint;
    private LineRenderer _grappleLine;


    public PlayerGrappleState(PlayerStateMachine context, PlayerStateFactory factory) : base(context, factory)
    {
        IsRootState = true;
    }

    public override void AdditionalUpdateLogic()
    {
        _grappleLine.SetPosition(0, Context.transform.position);
        Movement.BasicDirectionalMove(Context.Rigidbody, Context.LastMovementDirection, Context.CurrentSpeed, Context.CurrentAcceleration);
    }

    public override void CheckStateSwitch()
    {
        if (Context.IsGrapplePressed)
        {

        }else if (Context.IsGrounded)
        {
            SwitchState(Factory.Grounded());
        }
        else
        {
            SwitchState(Factory.Falling());
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entered Grapple State.");

        _joint = Context.gameObject.AddComponent<SpringJoint>();
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Context.GrappleHit.point;
        _joint.spring = Context.GrappleSpringConst;
        _joint.maxDistance = Context.MaxGrappleLength;
        _joint.minDistance = Context.MinGrappleLength;

        _grappleLine = Context.gameObject.AddComponent<LineRenderer>();
        _grappleLine.widthMultiplier = .1f;
        //_grappleLine.material = Context.GrappleMaterial;
        _grappleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _grappleLine.positionCount = 2;
        _grappleLine.SetPositions(new[] { Context.CinemachineBrain.transform.position, Context.GrappleHit.point });
        _joint.spring = 4.5f;
        _joint.damper = 7f;
        _joint.massScale = 4.5f;

        //Move toward Grapple Point
        Vector3 distanceFromGrappleHit = Context.GrappleHit.point - Context.transform.position;
        if(distanceFromGrappleHit.magnitude > Context.MaxGrappleLength)
            Context.transform.DOMove(Context.transform.position + distanceFromGrappleHit.normalized * (distanceFromGrappleHit.magnitude - Context.MaxGrappleLength), 1f).SetEase(Ease.OutCirc);
        {
        }
        Context.CurrentSpeed = Context.GrappleSpeed;
    }

    public override void ExitState()
    {
        Debug.Log("Exited Grapple State.");
        Context.DestroyComponent(_joint);
        Context.DestroyComponent(_grappleLine);

    }

    public override void InitializeSubState()
    {
    }
}