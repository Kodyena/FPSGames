using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public interface IDamageable
{
    public void Hit(float damage);

    public void Die();
}
