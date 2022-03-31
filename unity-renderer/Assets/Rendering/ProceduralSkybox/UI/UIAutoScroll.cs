using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIAutoScroll : MonoBehaviour
{
    public Camera UICam;
    public GameObject originalText;
    public Vector2 speed;

    List<Transform> elements = new List<Transform>();

    public Vector2 endPos;
    Vector3 _originalPos;

    void Start()
    {
        _originalPos = originalText.transform.position;
        elements.Add(originalText.transform);

        GameObject newText = Instantiate(originalText, originalText.transform.position, originalText.transform.rotation, originalText.transform.parent);
        newText.transform.position += new Vector3(-endPos.x, -endPos.y, 0);

        elements.Add(newText.transform);
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
            t.position += transform.right * speed.x * Time.deltaTime;
            t.position += transform.up * speed.y * Time.deltaTime;
        }
    }

    void ResetPosition()
    {
        if (Mathf.Abs(endPos.y - elements[0].position.y) <= 0.05f ||
            Mathf.Abs(endPos.x - elements[0].position.x) <= 0.05f)
        {
            Transform tempTransform = elements[0];
            elements.Remove(tempTransform);
            tempTransform.position = _originalPos;
            tempTransform.position += new Vector3(-endPos.x, -endPos.y, 0);
            elements.Add(tempTransform);
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_originalPos + new Vector3(endPos.x, endPos.y, 0), 0.5f);
    }
}
