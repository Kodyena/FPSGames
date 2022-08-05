using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IWeapon
{
    public float CalculateDamage();
    public void Fire(InputAction.CallbackContext context);

}
