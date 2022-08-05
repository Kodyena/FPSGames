using Cinemachine;
using System;
using System.Linq;
using System.Collections;
using UnityEngine;

public class CinemachineCameraHandler : MonoBehaviour
{

    private CinemachineVirtualCamera[] _cinemachineVirtualCameras;
    private CinemachineVirtualCamera _currentCamera;

    // Use this for initialization
    void Start()
    {
        _cinemachineVirtualCameras = GameObject.FindGameObjectsWithTag("VCam").Select(o => o.GetComponent<CinemachineVirtualCamera>()).ToArray();
        foreach (CinemachineVirtualCamera camera in _cinemachineVirtualCameras)
        {
            SwitchCameraTo("DefaultCamera");
        }
    }

    public void SwitchCameraTo(String cameraName)
    {
        foreach(CinemachineVirtualCamera camera in _cinemachineVirtualCameras)
        {
            if(camera.gameObject.name == cameraName)
            {
                camera.gameObject.SetActive(true);
            }
            else
            {
                camera.gameObject.SetActive(false);
            }
        }
    }
}