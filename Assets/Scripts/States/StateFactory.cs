using System.Collections;
using UnityEngine;

public abstract class StateFactory
{
    private PlayerStateMachine _context;
    public PlayerStateMachine Context { get { return _context; } }

    public StateFactory(PlayerStateMachine context)
    {
        _context = context;
    }
}