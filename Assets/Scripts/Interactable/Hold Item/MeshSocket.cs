using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Sets an attach point on the object requires an 'offset' child in 
 * order to effectively position into the socket
 */
public class MeshSocket : MonoBehaviour
{

    public MeshSockets.SocketId socketId;
    private Transform attachPoint;

    // Start is called before the first frame update
    void Start()
    {
        attachPoint = transform.GetChild(0);
    }

    public void Attach(Transform objectTransform)
    {
        objectTransform.SetParent(attachPoint,false);
    }
}
