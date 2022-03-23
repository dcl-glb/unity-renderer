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

    Material _frameMat;
    Material _backgroundMat;

    Animator _anim;

    void Start()
    {
        _frameMat = new Material(frameImg.material);
        frameImg.material = _frameMat;

        _backgroundMat = new Material(backgroundImg.material);
        backgroundImg.material = _backgroundMat;

        _anim = GetComponent<Animator>();
    }

    
    void Update()
    {
        _frameMat.SetFloat("_fill", frameFill);
        _backgroundMat.SetFloat("_visibility", fluidVisibility);
    }

    public void ToggleState(bool over)
    {
        if(over)
        {
            _anim.SetInteger("state", 1);
        }
        else
        {
            _anim.SetInteger("state", 2);
        }
    }
}
