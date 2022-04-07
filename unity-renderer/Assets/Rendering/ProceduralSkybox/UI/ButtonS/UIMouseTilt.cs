using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMouseTilt : MonoBehaviour
{
    public Camera cam;

    public GameObject target;
    public float tilt;
    public float distortion;

    bool _mouseOver;
    
    Vector2 _screenPos;

    RectTransform _rect;
    UIImageDistortion _img;

    private void Start()
    {
        _rect = GetComponent<RectTransform>();
        _img = GetComponent<UIImageDistortion>();
    }


    void Update()
    {
        Vector2 mousePos = cam.ScreenToViewportPoint(Input.mousePosition);
        
        if(_mouseOver)
        {

            target.transform.rotation = Quaternion.Euler((mousePos.y - _screenPos.y) * tilt, (mousePos.x - _screenPos.x) * tilt, 0);
            Resize(mousePos.x - _screenPos.x, mousePos.y - _screenPos.y);
        }
        else
        {
            target.transform.rotation = Quaternion.Euler(0, 0, 0);
            Resize(0,0);
        }
    }

    private void LateUpdate()
    {
        _screenPos = cam.WorldToViewportPoint(_rect.position);
    }

    void Resize(float x, float y)
    {
        if(x > 0)
        {
            _img.upperRight = new Vector2(_img.upperRight.x, x * distortion);
            _img.lowerRight = new Vector2(_img.lowerRight.x, -x * distortion);
        }
        else
        {
            _img.upperLeft = new Vector2(_img.upperLeft.x, -x * distortion);
            _img.lowerLeft = new Vector2(_img.lowerLeft.x, x * distortion);
        }

        if (y > 0)
        {
            _img.upperLeft = new Vector2(-y * distortion, _img.upperLeft.y);
            _img.upperRight = new Vector2(y * distortion, _img.upperRight.y);
        }
        else
        {
            _img.lowerLeft = new Vector2(y * distortion, _img.lowerLeft.y);
            _img.lowerRight = new Vector2(-y * distortion, _img.lowerRight.y);
        }

        if(x == 0 && y == 0)
        {
            _img.upperLeft = new Vector2(0, 0);
            _img.upperRight = new Vector2(0, 0);
            _img.lowerLeft = new Vector2(0, 0);
            _img.lowerRight = new Vector2(0, 0);
        }
        _img.SetVerticesDirty();
    }

    public void ToggleMouseOver(bool value)
    {
        _mouseOver = value;
    }
}
