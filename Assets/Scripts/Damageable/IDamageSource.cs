using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageSource
{
    public GameObject m_originator { get; }
    [SerializeField] 
    public float m_baseDamage { get; }
    [SerializeField] 
    public float m_criticalChance { get; }
    public RaycastHit m_hitInfo { get; }

    public float CalculateDamage();
}
