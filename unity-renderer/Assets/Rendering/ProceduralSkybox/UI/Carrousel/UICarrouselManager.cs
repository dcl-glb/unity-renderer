using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICarrouselManager : MonoBehaviour
{
    public float onScreenTime;
    float _currentOnScreenTime;
     
    public float transitionSpeed;
    public float fillSpeed;

    public float framePos;

    public List<Image> elements;


    List<Animator> _animators = new List<Animator>();
    Vector2 _midPos;
    bool _moving;
    void Start()
    {
        foreach (Image e in elements)
        {
            _animators.Add(e.gameObject.GetComponent<Animator>());
            e.fillAmount = 0f;
            e.fillOrigin = 0;
        }

        elements[0].fillAmount = 1f;
        elements[0].fillOrigin = 1;

        _currentOnScreenTime = onScreenTime;


        _midPos = Camera.main.WorldToViewportPoint(transform.position);
    }
    
    void Update()
    {
        if(_currentOnScreenTime <= 0 )
        {
            _moving = true;
        }
        else if(!_moving)
        {
            _currentOnScreenTime -= Time.deltaTime;
        }

        MoveElements();
    }
    
    void MoveElements()
    {
        if(_moving)
        {
            elements[0].transform.position -= elements[0].transform.right * transitionSpeed * Time.deltaTime;
            elements[0].fillAmount -= fillSpeed * Time.deltaTime;

            elements[1].transform.position -= elements[1].transform.right * transitionSpeed * Time.deltaTime;
            elements[1].fillAmount += fillSpeed * Time.deltaTime;
            
            foreach(Animator a in _animators)
            {
                a.SetInteger("state", 2);
            }

            if (Mathf.Abs(_midPos.x - Camera.main.WorldToViewportPoint(elements[1].transform.position).x) <= 0.05f)
            {
                foreach (Animator a in _animators)
                {
                    a.SetInteger("state", 1);
                }

                _moving = false;
                _currentOnScreenTime = onScreenTime;


                Image tempElement = elements[0];
                elements.Remove(elements[0]);

                elements[0].fillOrigin = 1;
                tempElement.transform.position = elements[elements.Count - 1].transform.position;
                tempElement.fillOrigin = 0;

                elements.Add(tempElement);
            }
        }    
    }
}
