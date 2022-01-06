using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxPlane3DManager : MonoBehaviour
{
    public List<GameObject> objectsToSpawn = new List<GameObject>();

    public float frequency;
    float currentTime;


    public float speed;
    public float distance;

    GameObject destroyer;

    public float width;

    public Vector3 objectsScale;
    public Vector3 objectsRotation;

    List<GameObject> spawned = new List<GameObject>();

    void Start()
    {
        currentTime = frequency;
        SpawnDestroyer();
    }

    void Update()
    {
        destroyer.transform.localPosition = new Vector3(0, 0, distance);

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else
        {
            SpawnObject();
            currentTime = frequency;
        }

        for (int i = 0; i < spawned.Count; i++)
        {
            if(spawned[i])
            {
                MoveObject(spawned[i]);
            }
        }
    }

    void SpawnDestroyer()
    {
        destroyer = new GameObject("Destroyer");
        destroyer.transform.SetParent(transform);
        destroyer.transform.localPosition = new Vector3(0, 0, distance);
    }

    void SpawnObject()
    {
        int objIndex = SetRandomIndex();
        Vector3 pos = SetRandomPosition() + transform.position;


        GameObject newObj = Instantiate(objectsToSpawn[objIndex], pos, transform.rotation, transform);
        spawned.Add(newObj);
    }

    int SetRandomIndex()
    {
        return Random.Range(0, objectsToSpawn.Count);
    }

    Vector3 SetRandomPosition()
    {
        float randomOffset = Random.Range(-width/2, width/2);

        Vector3 offset = transform.right * randomOffset;
        return offset;
    }

    void MoveObject(GameObject obj)
    {
        obj.transform.position += transform.forward * speed * Time.deltaTime;
        obj.transform.rotation = Quaternion.Euler(objectsRotation);
        obj.transform.localScale = objectsScale;

        if (obj.transform.localPosition.z >= destroyer.transform.localPosition.z)
        {
            DestroyObject(obj);
        }
    }

    void DestroyObject(GameObject obj)
    {
        spawned.Remove(obj);
        Destroy(obj);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 lowLeft = (transform.right * -width / 2) + transform.position;
        Vector3 lowRight = (transform.right * width / 2) + transform.position;


        Vector3 highLeft = (transform.forward * distance) + lowLeft;
        Vector3 highRight = (transform.forward * distance) + lowRight;

        Gizmos.DrawLine(lowLeft, lowRight);
        Gizmos.DrawLine(highLeft, highRight);

        Gizmos.DrawLine(lowLeft, highLeft);
        Gizmos.DrawLine(lowRight, highRight);
    }
}
