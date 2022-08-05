using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    public Vector3 m_recoilAngleShift;
    public Vector3 m_recoilPositionShift;
    public float m_recoilTime;

    private Vector3 m_startPosition;
    
    // Start is called before the first frame update
    void Start()
    {
        m_startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
