using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{

    [SerializeField] private Transform m_interactionPoint;
    [SerializeField] private float m_interactionPointRadius = 0.5f;
    [SerializeField] private LayerMask m_interactableMask;

    private Collider[] m_colliders;
    private List<Collider> previousColliders = new List<Collider>();

    [SerializeField] private int m_numFound;


    PlayerInputActions m_actions;

    private void OnEnable()
    {
        m_actions = new PlayerInputActions();
        m_actions.Player.Enable();
        m_actions.Player.Interact.started += OnInteract;
    }

    private void OnDisable()
    {
        m_actions.Player.Interact.started -= OnInteract;
        m_actions.Player.Disable();
    }

    private void Update()
    {
        m_colliders = new Collider[3];
        m_numFound = Physics.OverlapSphereNonAlloc(m_interactionPoint.position, m_interactionPointRadius, m_colliders, m_interactableMask);
        List<Collider> collidersToRemove = new List<Collider>();

        foreach (Collider c in previousColliders)
        {
            if (!m_colliders.Contains(c))
            {
                c.GetComponent<IInteractable>().ToggleHighlight(false);
                collidersToRemove.Add(c);
            }
        }

        previousColliders.RemoveAll(c => collidersToRemove.Contains(c));

        foreach (Collider col in m_colliders)
        {
            if (col != null && col.GetComponent<IInteractable>() != null)
            {
                if (!previousColliders.Contains(col))
                {
                    previousColliders.Add(col);
                    col.GetComponent<IInteractable>().ToggleHighlight(true);
                }
            }
        }
    }
    private void OnInteract(InputAction.CallbackContext context)
    {

        if (m_numFound > 0)
        {
            var interactable = m_colliders[0].GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(this);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(m_interactionPoint.position, m_interactionPointRadius);
    }
}
