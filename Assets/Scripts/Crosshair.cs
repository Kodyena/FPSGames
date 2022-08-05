using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{

    public Texture2D _crosshairTex;
    public float _crosshairScale;

    void OnGUI()
    {
        if(Time.timeScale != 0 && _crosshairTex != null)
        {
            GUI.DrawTexture(new Rect((Screen.width - _crosshairTex.width * _crosshairScale )/ 2, (Screen.height - _crosshairTex.height * _crosshairScale )/ 2, _crosshairTex.width*_crosshairScale, _crosshairTex.height *_crosshairScale), _crosshairTex);
        }
    }
}
