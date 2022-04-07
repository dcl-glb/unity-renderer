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
        gradient = EditorGUILayout.GradientField("Gradient", gradient);
    }
}
