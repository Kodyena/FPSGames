using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Goes on player object in order to store the currently held item
 */
public class PlayerHeldItem : MonoBehaviour
{

    [SerializeField] private HeldItem m_currentItem;
    MeshSockets m_scokets;

    private void Start()
    {
        m_scokets = GetComponent<MeshSockets>();
    }

    public void EquipItem(HeldItem item)
    {
        m_currentItem = item;
        m_scokets.Attach(item.transform, MeshSockets.SocketId.Spine);
    }

    public bool HasItem()
    {
        return m_currentItem != null;
    }
}

