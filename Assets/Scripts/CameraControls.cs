using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public Camera[] camList;

    public Transform[] camView;

    public Transform firstPersonView;

    private int currentIndex = 0;

    private bool[] firstPerson;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camList[currentIndex].enabled = true;
        firstPerson = new bool[camList.Length];
        
        for(int i = 0; i < camList.Length; i++)
        {
            camList[i].transform.SetPositionAndRotation(camView[i].position, camView[i].rotation);

            firstPerson[i] = false;

            if(i != currentIndex)
            {
                camList[i].enabled = false;
                camList[i].usePhysicalProperties = true;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            switchCamera(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            switchCamera(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            switchCamera(2);
        }



        if (Input.GetKey(KeyCode.Z))
        {
            camList[currentIndex].focalLength = camList[currentIndex].focalLength - 0.1f;
        }
        else if(Input.GetKey(KeyCode.X))
        {
            camList[currentIndex].focalLength = camList[currentIndex].focalLength + 0.1f;
        }

        if (Input.GetKey(KeyCode.C))
        {
            camList[currentIndex].sensorSize = new Vector2(camList[currentIndex].sensorSize.x - 0.1f, camList[currentIndex].sensorSize.y);
        }
        else if (Input.GetKey(KeyCode.V))
        {
            camList[currentIndex].sensorSize = new Vector2(camList[currentIndex].sensorSize.x + 0.1f, camList[currentIndex].sensorSize.y);
        }

        if (Input.GetKey(KeyCode.B))
        {
            camList[currentIndex].sensorSize = new Vector2(camList[currentIndex].sensorSize.x, camList[currentIndex].sensorSize.y - 0.1f);
        }
        else if (Input.GetKey(KeyCode.N))
        {
            camList[currentIndex].sensorSize = new Vector2(camList[currentIndex].sensorSize.x, camList[currentIndex].sensorSize.y + 0.1f);
        }
        
        if(Input.GetKeyDown(KeyCode.F))
        {
            if (firstPerson[currentIndex])
            {
                camList[currentIndex].transform.SetPositionAndRotation(camView[currentIndex].position, camView[currentIndex].rotation);
                firstPerson[currentIndex] = false;
            }
            else
            {
                camList[currentIndex].transform.SetPositionAndRotation(firstPersonView.position, firstPersonView.rotation);
                firstPerson[currentIndex] = true;
            }
        }
    }

    void switchCamera(int index)
    {
        camList[currentIndex].enabled = false;
        
        currentIndex = index;
        camList[currentIndex].enabled = true;
    }
}
