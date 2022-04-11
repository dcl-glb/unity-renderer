using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

public class RainbowInspector : ShaderGUI
{
    Gradient gradient = new Gradient();

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        /*Material targetMat = materialEditor.target as Material;
        
        gradient = EditorGUILayout.GradientField("Gradient", gradient);

        targetMat.SetFloat("_ColorAmount", gradient.colorKeys.Length);

        for(int i = 0; i < gradient.colorKeys.Length; i++)
        {
            targetMat.SetColor("_Color0" + (i + 1).ToString(), gradient.colorKeys[i].color);
        }*/
    }
}
