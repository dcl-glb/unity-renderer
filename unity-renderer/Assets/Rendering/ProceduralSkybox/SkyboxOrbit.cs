using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkyboxOrbit
{
    public float xAxis;
    public float yAxis;

    public SkyboxOrbit(float xAxis, float yAxis)
    {
        this.xAxis = xAxis;
        this.yAxis = yAxis;
    }

    public Vector2 Evaluate(float t)
    {
        float angle = Mathf.Deg2Rad * 360 * t; //GIVE US THE T POSITION IN THE ORBIT

        float x = Mathf.Sin(angle) * xAxis;
        float y = Mathf.Cos(angle) * yAxis;

        return new Vector2(x, y);
    }
}
