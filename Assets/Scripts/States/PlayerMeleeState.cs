using System.Collections;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class PlayerMeleeState : PlayerBaseState
{
    private bool _meleeComplete;

    public PlayerMeleeState(PlayerStateMachine context, PlayerStateFactory factory) : base(context, factory)
    {
        IsRootState = true;
    }

    public override void AdditionalUpdateLogic()
    {
    }

    public override void CheckStateSwitch()
    {
        SwitchState(Factory.Falling());
    }

    public override void EnterState()
    {
        Debug.Log("Performed Melee.");
        //_meleeComplete = false;
        //Context.transform.DOLocalMove(Context.transform.position + Context.transform.forward * 2, .1f).SetEase(Ease.OutCubic).SetUpdate(UpdateType.Fixed).OnComplete(() => _meleeComplete = true);
        Context.Rigidbody.velocity = Context.transform.forward * Context.MeleeSpeed;
        Collider[] overlapColliders = Physics.OverlapSphere(Context.transform.position, Context.MeleeInfluenceRadius);

        foreach (Collider col in overlapColliders)
        {
            if (col.attachedRigidbody && !col.attachedRigidbody.isKinematic && col.gameObject != Context.gameObject)
            {
                Rigidbody rigidbody = col.attachedRigidbody;
                Vector3 dir = col.transform.position - Context.transform.position;

                rigidbody.AddExplosionForce(Context.MeleeForce, Context.transform.position, Context.MeleeInfluenceRadius);
            }
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exited Melee State.");
    }

    public override void InitializeSubState()
    {
    }
}