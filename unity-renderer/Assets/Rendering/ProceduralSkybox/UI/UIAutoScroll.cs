using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIAutoScroll : MonoBehaviour
{
    public GameObject originalText;
    public Vector2 speed;

    List<Transform> elements = new List<Transform>();

    public Vector2 endPos;
    Vector2 _finalEndPos;
    Vector3 _originalPos;

    void Start()
    {
        _originalPos = originalText.transform.position;
        elements.Add(originalText.transform);

        GameObject newText = Instantiate(originalText, originalText.transform.position, originalText.transform.rotation, originalText.transform.parent);
        newText.transform.position += new Vector3(-endPos.x, -endPos.y, 0);

        elements.Add(newText.transform);

        _finalEndPos = endPos;
    }

    
    void Update()
    {
        if((speed.x < 0 && _finalEndPos.x > 0) || (speed.x > 0 && _finalEndPos.x < 0))
        {
            _finalEndPos = new Vector2(-_finalEndPos.x, _finalEndPos.y);
        }
        if ((speed.y < 0 && _finalEndPos.y > 0) || (speed.y > 0 && _finalEndPos.y < 0))
        {
            _finalEndPos = new Vector2(_finalEndPos.x, -_finalEndPos.y);
        }

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
        if (Mathf.Abs(_finalEndPos.y - elements[0].position.y) <= 0.05f ||
            Mathf.Abs(_finalEndPos.x - elements[0].position.x) <= 0.05f)
        {
            Transform tempTransform = elements[0];
            elements.Remove(tempTransform);
            tempTransform.position = _originalPos;
            tempTransform.position += new Vector3(-_finalEndPos.x, -_finalEndPos.y, 0);
            elements.Add(tempTransform);
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_originalPos + new Vector3(_finalEndPos.x, _finalEndPos.y, 0), 0.5f);
    }
}
