using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonCamera : MonoBehaviour
{
    UIFullButtonProperties _oButtonProps;
    UIFullButtonProperties _pButtonProps;

    bool mouseOver;

    public Camera renderCamera;
    public Camera targetCamera;

    public GameObject rawImg;
    public GameObject target;
    
    public List<Image> originalButtons = new List<Image>();

    private void Start()
    {
        _oButtonProps = GetComponent<UIFullButtonProperties>();
        _pButtonProps = target.GetComponent<UIFullButtonProperties>();
    }

    void Update()
    {
        if (mouseOver)
        {
            rawImg.SetActive(true);

            _pButtonProps.icon = _oButtonProps.icon;

            foreach (Image i in originalButtons)
            { 
                i.color = new Color(i.color.r, i.color.g, i.color.b, 0);
            }
        }
        else
        {
            rawImg.SetActive(false);

            foreach (Image i in originalButtons)
            {
                i.color = new Color(i.color.r, i.color.g, i.color.b, 1);
            }
        }
    }

    public void ToggleMouseOver(bool value)
    {
        mouseOver = value;
    }
}
