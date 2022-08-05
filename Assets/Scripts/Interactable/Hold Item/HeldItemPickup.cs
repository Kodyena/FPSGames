using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeldItemPickup : IInteractable
{

    public HeldItem itemFab;

    public Material m_highlightMaterial => throw new System.NotImplementedException();

    public string m_InteractionPrompt => throw new System.NotImplementedException();

    public void Highlight()
    {
        //TODO implement
    }

    public override bool Interact(Interactor interactor)
    {
        PlayerHeldItem heldItem = interactor.gameObject.GetComponent<PlayerHeldItem>();
        if (heldItem != null)
        {
            HeldItem newItem = Instantiate(itemFab);
            heldItem.EquipItem(newItem);
            Debug.Log("Item Equipped: " + newItem.m_itemName);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    public void UnHighlight()
    {
        //TODO implement
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
