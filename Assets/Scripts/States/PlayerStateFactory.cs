using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateFactory : StateFactory
{
    public PlayerStateFactory(PlayerStateMachine context) : base(context)
    {
    }

    public PlayerBaseState Idle()
    {
        return new PlayerIdleState(Context, this);
    }

    public PlayerBaseState Walk()
    {
        return new PlayerWalkState(Context, this);
    }
    public PlayerBaseState Melee()
    {
        return new PlayerMeleeState(Context, this);
    }

    public PlayerBaseState Run()
    {
        return new PlayerRunState(Context, this);
    }

    public PlayerBaseState Jump()
    {
        return new PlayerJumpState(Context, this);
    }

    public PlayerBaseState WallRun()
    {
        return new PlayerWallRunState(Context, this);
    }

    public PlayerBaseState Crouch()
    {
        return new PlayerCrouchState(Context, this);
    }

    public PlayerBaseState Grounded()
    {
        return new PlayerGroundedState(Context, this);
    }

    public PlayerBaseState Falling()
    {
        return new PlayerFallingState(Context, this);
    }

    internal PlayerBaseState Sliding()
    {
        return new PlayerSlidingState(Context, this);
    }

    public PlayerBaseState Grappling()
    {
        return new PlayerGrappleState(Context, this);
    }
}
