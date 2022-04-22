using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UIImageDistortion : Image
{
    public Vector2 upperLeft;
    public Vector2 upperRight;
    public Vector2 lowerLeft;
    public Vector2 lowerRight;

    VertexHelper verHelp;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);
        verHelp = vh;
        
        RelocateVertex(vh, 0, lowerLeft);
        RelocateVertex(vh, 1, upperLeft);
        RelocateVertex(vh, 2, upperRight);
        RelocateVertex(vh, 3, lowerRight);
    }

    public void RelocateVertex(VertexHelper vh, int index, Vector2 offset)
    {
        UIVertex vert = UIVertex.simpleVert;
        vh.PopulateUIVertex(ref vert, index);

        vert.position  += new Vector3(offset.x, offset.y, 0);
        vh.SetUIVertex(vert, index);
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (verHelp != null)
        {
            OnPopulateMesh(verHelp);
        }
    }
}
