using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMotionBlurProperties : MonoBehaviour
{
    Material _mat;

    public Color color = Color.black;
    [Range(0f, 1f)]
    public float blurIntensity = 0f;
    [Range(0.01f, 0.1f)]
    public float standarDaviation = 0f;
    public int samples = 1;


    private void Start()
    {
        Image img = GetComponent<Image>();
        _mat = new Material(img.material);
        img.material = _mat;
    }


    void Update()
    {
        _mat.SetColor("_color", color);
        _mat.SetFloat("_blurIntensity", blurIntensity);
        _mat.SetFloat("_standarDeviation", standarDaviation);
        _mat.SetFloat("_samples", samples);
    }
}
