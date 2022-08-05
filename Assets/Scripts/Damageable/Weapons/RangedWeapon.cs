using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;

 public abstract class RangedWeapon : MonoBehaviour, IWeapon
{

    public float m_baseDamage;
    public float m_critChance;

    public int m_maxAmmo;
    [SerializeField] protected int m_currentAmmo;
    public float m_reloadTime;
    public float m_fireRate;

    public bool m_isAutomatic;

    public GameObject m_fireLocation;

    private PlayerInputActions m_inputActions;

    private void Awake()
    {
        m_inputActions = new PlayerInputActions();
        m_inputActions.Player.Enable();
        m_inputActions.Player.Reload.started += ctx => Reload();
        m_inputActions.Player.Fire.started += Fire;
        m_inputActions.Player.Fire.canceled += Fire;
    }

    public abstract float CalculateDamage();

    public abstract void Fire(InputAction.CallbackContext context);

    public bool Reload()
    {
        if(m_currentAmmo < m_maxAmmo)
        {
            m_currentAmmo = m_maxAmmo;
            Debug.Log("Reloaded");
            return true;
        }
        else
        {
            Debug.Log("Already max ammo");
        }

        return false;
    }
}
