using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HitScanWeapon : RangedWeapon
{
    public ParticleSystem m_bulletHitEffect;
    public ParticleSystem m_muzzleFlare;

    public TrailRenderer m_trailRenderer;

    private RaycastHit m_lastHit;

    public override float CalculateDamage() { return m_baseDamage; }

    private void Start()
    {
        m_fireLocation = transform.GetChild(0).gameObject;
    }

    public override void Fire(InputAction.CallbackContext context)
    {
        if(m_currentAmmo <= 0)
        {
            Debug.Log("No Ammo");
            return;
        }

        m_currentAmmo--;

        ParticleSystem muzzleFlash = Instantiate(m_muzzleFlare);
        muzzleFlash.transform.SetParent(m_fireLocation.transform,true);
        //muzzleFlash.transform.localScale = Vector3.one;
        muzzleFlash.transform.localPosition = Vector3.zero;
        
        Camera playerCamera = Camera.main;
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out m_lastHit);

        
        if (m_lastHit.collider != null)
        {
            ParticleSystem bulletHit = Instantiate(m_bulletHitEffect);
            bulletHit.transform.position = m_lastHit.point;
            bulletHit.transform.forward = m_lastHit.normal;
            //TrailRenderer trail = Instantiate(m_trailRenderer);
            //trail.SetPositions(new Vector3[] { m_fireLocation.transform.position, m_lastHit.point });
            //trail.enabled = true;

            IDamageable damageable = m_lastHit.collider.gameObject.GetComponent<IDamageable>();

            if(damageable != null)
            {
                damageable.Hit(CalculateDamage());
            }
        }
    }
}
