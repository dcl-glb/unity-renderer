using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonStates : MonoBehaviour
{
    public Image target;
    
    public Color colorBase;
    public Color colorHovered;
    public Color colorSelected;

    public float hoveredScale;

    public ParticleSystem particles;

    Material mat;
    RectTransform rect;
    Vector2 originalSize;

    Color offsetColor = new Color(0.2f, 0.2f, 0.2f, 0f);

    bool selected;
    private void Awake()
    {
        target.material = new Material(target.material);
        mat = target.material;
        rect = target.GetComponent<RectTransform>();
        originalSize = rect.sizeDelta;

        mat.SetColor("_color_01", colorBase);
        mat.SetColor("_color_02", colorBase - offsetColor);
    }

    public void Click()
    {
        Debug.Log("Click");

        if(selected)
        {
            selected = false;
            mat.SetColor("_color_01", colorHovered);
            mat.SetColor("_color_02", colorHovered - offsetColor);
        }
        else
        {
            selected = true;
            mat.SetColor("_color_01", colorSelected);
            mat.SetColor("_color_02", colorSelected - offsetColor);
        }

        if (particles)
        {
            particles.Stop();
            particles.Play();
        }
    }

    public void Over()
    {
        Debug.Log("Over");

        if (!selected)
        {
            mat.SetColor("_color_01", colorHovered);
            mat.SetColor("_color_02", colorHovered - offsetColor);
        }
        rect.sizeDelta = originalSize * hoveredScale;
    }

    public void Exit()
    {
        Debug.Log("Exit");

        if(!selected)
        {
            mat.SetColor("_color_01", colorBase);
            mat.SetColor("_color_02", colorBase - offsetColor);
        }

        rect.sizeDelta = originalSize;
    }
}
