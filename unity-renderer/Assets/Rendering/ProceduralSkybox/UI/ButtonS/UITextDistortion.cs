using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UITextDistortion : MonoBehaviour
{
    TMP_Text _text;
    Mesh _mesh;
    Vector3[] _vertices;

    public Vector2 upperLeft;
    public Vector2 upperRight;
    public Vector2 lowerLeft;
    public Vector2 lowerRight;

    private void Start()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        _text.ForceMeshUpdate();
        _mesh = _text.mesh;
        _vertices = _mesh.vertices;
        //Debug.Log(_vertices.Length);
    }
}
