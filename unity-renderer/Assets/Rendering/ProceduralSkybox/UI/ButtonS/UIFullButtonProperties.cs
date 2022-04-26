using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFullButtonProperties : MonoBehaviour
{
    [Header("Frame")]
    public Image frameImg;
    
    [Range(0, 1)]
    public float frameFill;
    [Range(0, 360)]
    public float frameStartAngle = 135f;
    public float frameGlowSize = 0.9f;

    [Header("Fill")]
    public Image backgroundImg;
    
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
            _frameMat.SetFloat("_Fill", frameFill);
            _frameMat.SetFloat("_StartingAngle", frameStartAngle);
            _frameMat.SetFloat("_GlowSampleSize", frameGlowSize);
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
                _anim.SetTrigger("In");
            }
            else
            {
                _anim.SetTrigger("Out");
            }
        }
        
    }
}
