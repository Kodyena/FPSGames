using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public abstract class PlayerBaseState 
{
    private PlayerStateMachine _context;
    private PlayerStateFactory _factory;
    protected PlayerBaseState _currentSubState;
    protected PlayerBaseState _currentSuperState;
    private bool _isRoot = false;

    public bool IsRootState { get { return _isRoot; } set => _isRoot = value; }
    protected PlayerStateMachine Context { get { return _context; } }
    public PlayerStateFactory Factory { get => _factory; set => _factory = value; }
    
    public PlayerBaseState(PlayerStateMachine context, PlayerStateFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void CheckStateSwitch();
    public abstract void InitializeSubState();

    protected void SetSuperState(PlayerBaseState newSuperState)
    {
        _currentSuperState = newSuperState;
    }

    protected void SetSubState(PlayerBaseState newSubState)
    {
        _currentSubState = newSubState;
        newSubState.SetSuperState(this);
    }

    public void UpdateState()
    {
        CheckStateSwitch();
        AdditionalUpdateLogic();
    }

    public void UpdateStates()
    {
        UpdateState();
        if (_currentSubState != null)
        {
            _currentSubState.UpdateStates();
        }
    }

    public void SwitchState(PlayerBaseState newState)
    {
        ExitState();
        newState.EnterState();

        if (_isRoot)
        {
            Context.CurrentMovementState = newState;
            if (_currentSubState != null) _currentSubState.ExitState();
        }
        else if (_currentSuperState != null)
        {
            _currentSuperState.SetSubState(newState);
        }
    }

    public abstract void AdditionalUpdateLogic();

}