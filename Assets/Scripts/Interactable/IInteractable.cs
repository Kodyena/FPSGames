using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public abstract class IInteractable : MonoBehaviour
{
    [SerializeReference] protected Color _highlightColor;
    [SerializeReference] protected float _emissionIntensity;
    [SerializeReference] private string _interactionPrompt;
    protected string InteractionPrompt { get => _interactionPrompt; }

    public abstract bool Interact(Interactor interactor);
    public void ToggleHighlight(bool isOn)
    {
        if (isOn)
        {
            foreach(Renderer rend in GetComponents<Renderer>())
            {
                rend.material.SetColor("_EmissiveColor", _highlightColor * 100);
            }

            InteractionPromptUI interactUI = GameObject.FindGameObjectsWithTag("InteractCanvas")[0].GetComponent<InteractionPromptUI>() ;
            interactUI.gameObject.transform.position = transform.position + Vector3.up * .5f;
            interactUI.PromptText.text = InteractionPrompt;
            interactUI.TogglePrompt(true);

        }
        else
        {
            foreach(Renderer rend in GetComponents<Renderer>())
            {
                rend.material.SetColor("_EmissiveColor", _highlightColor * 0);
                InteractionPromptUI interactUI = GameObject.FindGameObjectsWithTag("InteractCanvas")[0].GetComponent<InteractionPromptUI>();
                interactUI.TogglePrompt(false);
            }
        }
    }

}
