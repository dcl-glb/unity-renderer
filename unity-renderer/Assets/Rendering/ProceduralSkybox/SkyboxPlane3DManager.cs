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

    List<GameObject> spawnedMoving = new List<GameObject>();


    public bool isStatic;
    bool prevStatic;

    public int staticAmount;
    List<GameObject> spawnedStatic = new List<GameObject>();

    void Start()
    {
        prevStatic = isStatic;
        currentTime = frequency;
        SpawnDestroyer();
    }

    void Update()
    {
        destroyer.transform.localPosition = new Vector3(0, 0, distance);
        UpdateTranform();

        if (isStatic != prevStatic)
        {
            if(isStatic)
            {
                if (spawnedMoving.Count > 0)
                {
                    for(int i = spawnedMoving.Count - 1; i >= 0; i--)
                    {
                        DestroyObject(spawnedMoving[i], true);
                    }
                }
                SpawnStatics();
            }
            else
            {
                if (spawnedStatic.Count > 0)
                {
                    for (int i = spawnedStatic.Count - 1; i >= 0; i--)
                    {
                        DestroyObject(spawnedStatic[i], false);
                    }
                }
            }

            prevStatic = isStatic;
        }

        if(!isStatic)
        {
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
            }
            else
            {
                SpawnObject();
                currentTime = frequency;
            }

            for (int i = 0; i < spawnedMoving.Count; i++)
            {
                if(spawnedMoving[i])
                {
                    MoveObject(spawnedMoving[i]);
                }
            }
        }
    }

    void UpdateTranform()
    {
        if(isStatic)
        {
            foreach (GameObject o in spawnedStatic)
            {
                o.transform.rotation = Quaternion.Euler(objectsRotation);
                o.transform.localScale = objectsScale;
            }
        }
        else
        {
            foreach(GameObject o in spawnedMoving)
            {
                o.transform.rotation = Quaternion.Euler(objectsRotation);
                o.transform.localScale = objectsScale;
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
        spawnedMoving.Add(newObj);
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

        if (obj.transform.localPosition.z >= destroyer.transform.localPosition.z)
        {
            DestroyObject(obj, true);
        }
    }

    void SpawnStatics()
    {
        for(int i = 0; i < staticAmount; i++)
        {
            int index = SetRandomIndex();
            Vector3 pos = SetRandomSpawnPoint();

            GameObject newObj = Instantiate(objectsToSpawn[index], pos, transform.rotation, transform);
            spawnedStatic.Add(newObj);
        }
    }

    Vector3 SetRandomSpawnPoint()
    {
        float xPos = Random.Range(-width / 2, width / 2);
        float yPos = transform.position.y;
        float zPos = Random.Range(0, destroyer.transform.position.z);

        return new Vector3(xPos, yPos, zPos);
    }

    void DestroyObject(GameObject obj, bool moving)
    {
        if(moving)
        {
            spawnedMoving.Remove(obj);
        }
        else
        {
            spawnedStatic.Remove(obj);
        }

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
