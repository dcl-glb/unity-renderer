using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMouseTilt : MonoBehaviour
{
    public Camera cam;

    public GameObject tiltTarget;
    public float tilt;
    //public float distortion;

    bool _mouseOver;
    
    Vector2 _screenPos;

    RectTransform _rect;
    //UIImageDistortion[] _imgs;
    //public List<float> imgDistortMulti = new List<float>();


    private void Start()
    {
        _rect = GetComponent<RectTransform>();
        /*_imgs = GetComponentsInChildren<UIImageDistortion>();

        while (imgDistortMulti.Count < _imgs.Length)
        {
            imgDistortMulti.Add(1);
        }*/
    }


    void Update()
    {
        Vector2 mousePos = cam.ScreenToViewportPoint(Input.mousePosition);
        
        if(_mouseOver)
        {
            tiltTarget.transform.rotation = Quaternion.Euler((mousePos.y - _screenPos.y) * tilt, (mousePos.x - _screenPos.x) * tilt, 0);
            //CalculatePositions(mousePos.x - _screenPos.x, mousePos.y - _screenPos.y);
        }
        else
        {
            //tiltTarget.transform.rotation = Quaternion.Euler(0, 0, 0);
            //CalculatePositions(0,0);
        }
    }

    private void LateUpdate()
    {
        _screenPos = cam.WorldToViewportPoint(_rect.position);
    }

    /*void CalculatePositions(float x, float y)
    {
        Vector2 upperLeft = new Vector2(0,0);
        Vector2 upperRight = new Vector2(0, 0);
        Vector2 lowerRight = new Vector2(0, 0);
        Vector2 lowerLeft = new Vector2(0, 0);

        if (x > 0)
        {
            upperRight = new Vector2(_imgs[0].upperRight.x, x * distortion / 2);
            lowerRight = new Vector2(_imgs[0].lowerRight.x, -x * distortion / 2);

            upperLeft = new Vector2(_imgs[0].upperRight.x, -x * distortion / 2);
            lowerLeft = new Vector2(_imgs[0].lowerRight.x, x * distortion / 2);

            Distort(upperLeft, upperRight, lowerRight, lowerLeft);
        }
        else
        {
            upperRight = new Vector2(_imgs[0].upperRight.x, x * distortion / 2);
            lowerRight = new Vector2(_imgs[0].lowerRight.x, -x * distortion / 2);

            upperLeft = new Vector2(_imgs[0].upperLeft.x, -x * distortion / 2);
            lowerLeft = new Vector2(_imgs[0].lowerLeft.x, x * distortion / 2);

            Distort(upperLeft, upperRight, lowerRight, lowerLeft);
        }

        if (y > 0)
        {
            upperRight = new Vector2(y * distortion / 2, _imgs[0].upperRight.y);
            lowerRight = new Vector2(-y * distortion / 2, _imgs[0].lowerRight.y);

            upperLeft = new Vector2(-y * distortion / 2, _imgs[0].upperLeft.y);
            lowerLeft = new Vector2(y * distortion / 2, _imgs[0].lowerLeft.y);

            Distort(upperLeft, upperRight, lowerRight, lowerLeft);
        }
        else
        {
            upperRight = new Vector2(y * distortion / 2, _imgs[0].upperRight.y);
            lowerRight = new Vector2(-y * distortion / 2, _imgs[0].lowerRight.y);

            upperLeft = new Vector2(-y * distortion / 2, _imgs[0].upperLeft.y);
            lowerLeft = new Vector2(y * distortion / 2, _imgs[0].lowerLeft.y);

            Distort(upperLeft, upperRight, lowerRight, lowerLeft);
        }

        if(x == 0 && y == 0)
        {
            Distort(new Vector2(0,0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        }

        for (int i = 0; i < _imgs.Length; i++)
        {
            UIImageDistortion img = _imgs[i];
            img.SetVerticesDirty();
        }
    }

    void Distort(Vector2 upperLeft, Vector2 upperRight, Vector2 lowerRight, Vector2 lowerLeft)
    {
        for (int i = 0; i < _imgs.Length; i++)
        {
            UIImageDistortion img = _imgs[i];
            float intensity = imgDistortMulti[i];

            img.upperRight = upperRight * intensity;
            img.lowerRight = lowerRight * intensity;

            img.upperLeft = upperLeft * intensity;
            img.lowerLeft = lowerLeft * intensity;
        }
    }*/

    public void ToggleMouseOver(bool value)
    {
        _mouseOver = value;
    }
}
