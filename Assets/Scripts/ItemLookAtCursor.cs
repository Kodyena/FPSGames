using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemLookAtCursor : MonoBehaviour
{
    public float m_turnSpeed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera playerCamera = Camera.main;
        RaycastHit focusPoint;
        RaycastHit currentPoint;
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out focusPoint);
        Physics.Raycast(transform.position, transform.forward, out currentPoint);
        if (focusPoint.collider != null)
        {
            transform.LookAt(Vector3.Lerp(currentPoint.point, focusPoint.point,Time.deltaTime * m_turnSpeed ));
        }
    }
}
