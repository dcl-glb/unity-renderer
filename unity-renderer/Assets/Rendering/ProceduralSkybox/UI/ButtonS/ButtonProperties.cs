using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonProperties : MonoBehaviour
{
    public Image image;
    public Animator anim;
    public ParticleSystem particles;

    Material mat;

    public bool particlePlaying;

    public Color color01;
    public Color color02;
    public float rotation;
    public float amount;
    public Vector2 gridSize;
    public Vector2 speed;
    private void Awake()
    {

        image.material = new Material(image.material);
        mat = image.material;
    }
    void FixedUpdate()
    {

        mat.SetColor("_color_01", color01);
        mat.SetColor("_color_02", color02);

        mat.SetFloat("_rotation", rotation);
        mat.SetFloat("_amount", amount);

        mat.SetVector("_gridSize", gridSize);
        mat.SetVector("_speed", speed);

        if(particlePlaying && !particles.isPlaying)
        {
            particles.Play();
        }
    }
}
