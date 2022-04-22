using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFullButtonProperties : MonoBehaviour
{
    public Image frameImg;
    public Image backgroundImg;

    [Range(0, 1)]
    public float frameFill;

    [Range(0,1)]
    public float fluidVisibility;
    public Texture icon;
    public Color backgroundColor;
    public Color iconColor;


    Material _frameMat;
    Material _backgroundMat;

    Animator _anim;

    void Start()
    {
        if(frameImg)
        {
            _frameMat = new Material(frameImg.material);
            frameImg.material = _frameMat;
        }

        if(backgroundImg)
        {
            _backgroundMat = new Material(backgroundImg.material);
            backgroundImg.material = _backgroundMat;
        }

        _anim = GetComponent<Animator>();
    }

    
    void Update()
    {
        if(frameImg)
        {
            _frameMat.SetFloat("_fill", frameFill);
        }
        
        if(backgroundImg)
        {
            _backgroundMat.SetFloat("_Visibility", fluidVisibility);
            _backgroundMat.SetTexture("_Icon", icon);
            _backgroundMat.SetColor("_BaseColor", backgroundColor);
            _backgroundMat.SetColor("_IconColor", iconColor);
        }
    }

    public void ToggleState(bool over)
    {
        if(_anim)
        {
            if (over)
            {
                _anim.SetInteger("state", 1);
            }
            else
            {
                _anim.SetInteger("state", 2);
            }
        }
        
    }
}
