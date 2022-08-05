using DG.Tweening;
using UnityEditor.Rendering;
using UnityEngine;
using System;
using Cinemachine;

internal class PlayerWallRunState : PlayerBaseState
{
    private float _currentWallRunTime;
    private float _currentWallRunLength;
    private float _wallTiltAngle;

    public PlayerWallRunState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory) {
        IsRootState = true;
        InitializeSubState();
    }
    
    public override void CheckStateSwitch()
    {
        if (_currentWallRunLength > Context.MaxWallRunTime * Context.WallRunSpeed)
        {
            SwitchState(Factory.Falling());
        }else if (Context.IsJumpPressed)
        {
            SwitchState(Factory.Jump());
        }
    }

    public override void EnterState()
    {
        Debug.Log("Entered WallRunning State.");
        Context.Animator.SetTrigger(Context.IsWallRunningHash);
        _currentWallRunLength = 0;
        _currentWallRunTime = 0;
        _wallTiltAngle = Vector3.Dot(Context.WallHit.normal, Context.transform.right) > 0 ? -Context.WallTiltAngle : Context.WallTiltAngle;
        CinemachineVirtualCamera wallRunCamera = Array.Find(GameObject.FindGameObjectsWithTag("VCam"), x => x.name == "WallRunDutchCamera").GetComponent<CinemachineVirtualCamera>();
        //Context.CinemachineBrain.m_Lens.Dutch += _wallTiltAngle;
    }

    public override void ExitState()
    {
        Debug.Log("Entered WallRunning State.");
        Context.CurrentCMCamera.m_Lens.Dutch = 0;
    }

    public override void InitializeSubState()
    {
    }

    public override void AdditionalUpdateLogic()
    {
        _currentWallRunTime += Time.deltaTime;
        _currentWallRunLength = _currentWallRunTime * Context.WallRunSpeed;
        Context.Rigidbody.AddForce(-Context.WallHit.normal * 2, ForceMode.Force);

        Vector3 wallrunDir = Vector3.Cross(Vector3.up, Context.WallHit.normal).normalized;

        Vector3 wallVelocity = wallrunDir * Context.WallRunSpeed * ((Vector3.Dot(wallrunDir, Context.gameObject.transform.forward) > 0) ? 1.0f : -1.0f);
        float flatDecay = Context.WallRunDecayCurve.Evaluate(_currentWallRunTime / Context.MaxWallRunTime);
        float vertDecay = Context.WallRunFallCurve.Evaluate(_currentWallRunTime / Context.MaxWallRunTime);
        Context.Rigidbody.velocity = new Vector3(wallVelocity.x * flatDecay, Context.Rigidbody.velocity.y * vertDecay, wallVelocity.z * flatDecay);

        ClampCamera();
    }

    private void ClampCamera()
    {
        Vector3 normalDir = Context.WallHit.normal;
        Vector3 tangentDir = Vector3.Cross(Vector3.up, normalDir);
        Vector3 cameraForward = Context.transform.forward;
        tangentDir *= Vector3.Dot(tangentDir, cameraForward) > 0 ? 1.0f : -1.0f;
        normalDir = Quaternion.AngleAxis(90 - Context.WallRunMaxAngle, Vector3.Cross(normalDir, tangentDir)) * normalDir;

        //check rotations from norm to cam to tan are same and from tan to cam to norm are same
        if (Vector3.Dot(Vector3.Cross(tangentDir, cameraForward), Vector3.Cross(tangentDir, normalDir)) < 0 ||
            Vector3.Dot(Vector3.Cross(normalDir, cameraForward), Vector3.Cross(normalDir, tangentDir)) < 0)
        {
            Context.transform.forward = Vector3.AngleBetween(tangentDir, Context.CurrentCMCamera.transform.forward) < Vector3.AngleBetween(normalDir, Context.CurrentCMCamera.transform.forward) ? tangentDir : normalDir;
        }
    }
}