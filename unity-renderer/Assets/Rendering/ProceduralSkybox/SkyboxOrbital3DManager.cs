using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxOrbital3DManager : MonoBehaviour
{
    LineRenderer lr;

    public int segments;
    public SkyboxOrbit orbit;


    Vector3[] points;


    public Transform orbitingObj;
    public float orbitProgress;
    public float orbitPeriod;


    public bool lookAtOrbit;
    public Vector3 rotationSpeed;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        if(orbitingObj)
        {
            SetObjectPosition();
            StartCoroutine(MoveObject());
        }
    }

    void CalculateOrbit()
    {
        points = new Vector3[segments + 1];
        
        for (int i = 0; i < segments; i++)
        {
            Vector2 tPos = orbit.Evaluate((float)i / (float)segments);
            points[i] = new Vector3(tPos.x, tPos.y, 0f);
        }

        points[segments] = points[0]; //EQUALS THE LAST POINT TO THE FIRST TO HAVE A FULL ELLIPSE

        if(lr)
        { 
            lr.positionCount = segments + 1;
            lr.SetPositions(points);  
        }
    }

    private void Update()
    {
        if(!lookAtOrbit)
        {
            orbitingObj.transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        CalculateOrbit();
    }

    void SetObjectPosition()
    {
        Vector2 orbitPos = orbit.Evaluate(orbitProgress);

        Vector3 finalPos = new Vector3(orbitPos.x, orbitPos.y, 0);

        if (lookAtOrbit)
        {
            orbitingObj.LookAt(finalPos, orbitingObj.up);
        }

        orbitingObj.localPosition = finalPos;
    }

    IEnumerator MoveObject()
    {
        if(orbitPeriod == 0f)
        {
            orbitPeriod = 0.01f;
        }

        float orbitSpeed = 1f / orbitPeriod;

        while(true)
        {
            orbitProgress += orbitSpeed * Time.deltaTime;
            orbitProgress %= 1f;
            SetObjectPosition();
            yield return null;
        }
    }

    /*private void OnDrawGizmos()
    {
        CalculateOrbit();

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 pointA = transform.position + points[i];
            
            if (i != points.Length - 1)
            {
                Vector3 pointB = transform.position + points[i + 1];

                Gizmos.DrawLine(pointA, pointB);
            }
            else
            {
                Vector3 pointB = transform.position + points[0];

                Gizmos.DrawLine(pointA, pointB);
            }
        }
    }*/
}
