using Cinemachine;
using System.Collections;
using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    private CinemachineBrain _cinemachineBrain;
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _promptText;

    public TextMeshProUGUI PromptText { get => _promptText; set => _promptText = value; }

    // Use this for initialization
    void Start()
    {
        _cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        _uiPanel.SetActive(false);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Quaternion rot = _cinemachineBrain.transform.rotation;
        transform.LookAt(transform.position + rot * Vector3.forward, rot * Vector3.up);
    }

    public void TogglePrompt(bool toggle)
    {
        _uiPanel.SetActive(toggle);
    }
}