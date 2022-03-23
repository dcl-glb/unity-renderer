using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMouseTilt : MonoBehaviour
{
    public Camera cam;

    public GameObject target;
    public float tilt;

    bool _mouseOver;
    
    Vector2 _screenPos;

    RectTransform _rect;

    private void Start()
    {
        _rect = GetComponent<RectTransform>();
    }


    void Update()
    {
        Vector2 mousePos = cam.ScreenToViewportPoint(Input.mousePosition);
        
        if(_mouseOver)
        {

            target.transform.rotation = Quaternion.Euler((mousePos.y - _screenPos.y) * tilt, (mousePos.x - _screenPos.x) * tilt, 0);
        }
        else
        {
            target.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private void LateUpdate()
    {
        _screenPos = cam.WorldToViewportPoint(_rect.position);
    }

    public void ToggleMouseOver(bool value)
    {
        _mouseOver = value;
    }
}
