using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageableTest : MonoBehaviour, IDamageable
{
    public float m_maxHealth;

    [SerializeField] private float m_currentHealth;

    public void Hit(float damage)
    {
        m_currentHealth -= damage;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_currentHealth = m_maxHealth; 
    }

    public void Update()
    {
        if(m_currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log(this.name + " has been killed.");
        Destroy(gameObject);
    }
}
