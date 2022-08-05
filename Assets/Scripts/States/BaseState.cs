using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

public abstract class BaseState
{
    private PlayerStateMachine _context;
    private StateFactory _factory;
    protected BaseState _currentSubState;
    protected BaseState _currentSuperState;
    protected bool _isRoot = false;

    protected bool IsRootState { set { _isRoot = value; } }
    protected PlayerStateMachine Context { get { return _context; } }

    public BaseState(PlayerStateMachine context, StateFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void CheckStateSwitch();
    public abstract void InitializeSubState();

    protected void SetSuperState(BaseState newSuperState)
    {
        _currentSuperState = newSuperState;
    }

    protected void SetSubState(BaseState newSubState)
    {
        _currentSubState = newSubState;
        newSubState.SetSuperState(this);
    }

    public void UpdateState()
    {
        CheckStateSwitch();
    }

    public void UpdateStates()
    {
        UpdateState();
        if (_currentSubState != null)
        {
            _currentSubState.UpdateStates();
        }
    }

    public abstract void SwitchState(BaseState newState);
}