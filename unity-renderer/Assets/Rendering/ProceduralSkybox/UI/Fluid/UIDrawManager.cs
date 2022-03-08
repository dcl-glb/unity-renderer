using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDrawManager : MonoBehaviour
{
    public UIDrawFluid[] fluidDraws;

    public Shader drawShader;

    [Range(0.01f, 500f)]
    public float brushSize = 50;

    [Range(0f, 50f)]
    public float brushHardness = 1;

    public float brushSpacing = 1f;

    [Range(0.01f, 500f)]
    public float eraserSize = 50;

    [Range(0f, 50f)]
    public float eraserHardness = 1;


    Material _drawMaterial;
    Material _eraseMaterial;

    RenderTexture _rt;

    Vector2 _prevPos;
    Vector2 _currentPos;

    Queue<Vector2> _mouseBuffer = new Queue<Vector2>();

    float _currentTimer;
    float _maxTimer = 1f;


    private void Awake()
    {
        fluidDraws = GetComponentsInChildren<UIDrawFluid>();

        foreach(UIDrawFluid f in fluidDraws)
        {
            f.drawShader = drawShader;
            f.brushSize = brushSize;
            f.brushHardness = brushHardness;
            f.brushSpacing = brushSpacing;
            f.eraserSize = eraserSize;
            f.eraserHardness = eraserHardness;
        }

       _drawMaterial = new Material(drawShader);
        _drawMaterial.SetColor("_Color", Color.red);

        _eraseMaterial = new Material(drawShader);
        _eraseMaterial.SetColor("_Color", new Color(-1, 0, 0, 0));//Color.black) ;

        _rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        _currentTimer = _maxTimer;
    }

    private void Start()
    {
        foreach (UIDrawFluid f in fluidDraws)
        {
            f._finalMaterial.SetTexture("_Mask", _rt);
        }
    }

    private void FixedUpdate()
    {
        foreach (UIDrawFluid f in fluidDraws)
        {
            f.brushSize = brushSize;
            f.brushHardness = brushHardness;
            f.brushSpacing = brushSpacing;
            f.eraserSize = eraserSize;
            f.eraserHardness = eraserHardness;
        }
    }

    private void Update()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float counter = 0;
        foreach (UIDrawFluid f in fluidDraws)
        {
            if (f._isPainting)
            {
                counter ++;
            }
        }

        if (counter > 0)
        {
            _currentPos = Input.mousePosition;

            if (Vector2.Distance(_currentPos, _prevPos) > brushSpacing)
            {
                Debug.Log("Painting");

                _drawMaterial.SetFloat("_Size", brushSize);
                _drawMaterial.SetFloat("_Strength", brushHardness);

                Vector3 mousePos = Input.mousePosition;
                mousePos = new Vector3((mousePos.x / screenWidth), (mousePos.y / screenHeight), 0);

                _drawMaterial.SetVector("_Coordinate", mousePos);
                RenderTexture temp = RenderTexture.GetTemporary(_rt.width, _rt.height, 0, RenderTextureFormat.ARGBFloat);
                Graphics.Blit(_rt, temp, _drawMaterial);
                Graphics.Blit(temp, _rt);
                //Graphics.Blit(temp, _sm, _drawMaterial);

                //_finalMaterial.SetTexture("_Mask", _rt);

                temp.Release();

                _mouseBuffer.Enqueue(mousePos);
            }
        }

        if (_currentTimer >= 0)
        {
            _currentTimer -= Time.deltaTime;
        }
        else if (_mouseBuffer.Count > 0)
        {
            print("Erasing");

            _eraseMaterial.SetFloat("_Size", eraserSize);
            _eraseMaterial.SetFloat("_Strength", eraserHardness);

            Vector2 tempPos = _mouseBuffer.Dequeue();

            _eraseMaterial.SetVector("_Coordinate", tempPos);
            RenderTexture eraseTemp = RenderTexture.GetTemporary(_rt.width, _rt.height, 0, RenderTextureFormat.ARGBFloat);
            Graphics.Blit(_rt, eraseTemp, _eraseMaterial);
            Graphics.Blit(eraseTemp, _rt);
            //Graphics.Blit(temp, _sm, _drawMaterial);

            //_finalMaterial.SetTexture("_Mask", _rt);

            eraseTemp.Release();
        }

        if (_mouseBuffer.Count == 0)
        {
            _currentTimer = _maxTimer;
        }
    }

    private void LateUpdate()
    {
        _prevPos = Input.mousePosition;
    }
}
