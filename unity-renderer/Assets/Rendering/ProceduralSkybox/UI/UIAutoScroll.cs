using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIAutoScroll : MonoBehaviour
{
    public float speed;

    List<Transform> elements = new List<Transform>();

    public Vector2 endPos;

    void Start()
    {
        elements = GetComponentsInChildren<Transform>().ToList<Transform>();
        elements.Remove(elements[0]);

        //endPos = new Vector2(0, -Camera.main.WorldToViewportPoint(elements[1].position).y);
    }

    
    void Update()
    {
        MoveElements();
        ResetPosition();
    }

    void MoveElements()
    {
        foreach(Transform t in elements)
        {
            t.position += transform.up * speed * Time.deltaTime;
        }
    }

    void ResetPosition()
    {
        if (Mathf.Abs(endPos.y - Camera.main.WorldToViewportPoint(elements[0].position).y) <= 0.05f)
        {
            Transform tempTransform = elements[0];
            elements.Remove(tempTransform);
            tempTransform.position = new Vector3(tempTransform.position.x, -endPos.y, tempTransform.position.z);
            elements.Add(tempTransform);
        }
        
    }

}
