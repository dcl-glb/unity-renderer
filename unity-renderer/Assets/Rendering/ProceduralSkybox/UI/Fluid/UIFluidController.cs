using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFluidController : MonoBehaviour
{
    Image img;
    Material mat;
    Texture mapTex;
    Vector2 mapSize;
    public Vector2 offset;

    void Start()
    {
        img = GetComponent<Image>();
        img.material = new Material(img.material);
        mat = img.material;
        mapTex = mat.GetTexture("_MainTex");
        mapSize = new Vector2(mapTex.width, mapTex.height);
    }

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        mousePos = new Vector3 ((mousePos.x /screenWidth * 10) + offset.x, (mousePos.y / screenHeight * 5) + offset.y, 0);
        

        mat.SetVector("_offset", mousePos);
    }
}
