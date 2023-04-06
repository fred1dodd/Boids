using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private int counter = 0;

    private List<Vector3> camera_positions = new List<Vector3>(); 
    private List<Vector3> camera_rotations = new List<Vector3>();

    private void Awake()
    {
        Vector3 position1 = new Vector3(-5.0f, 30.0f, -60.0f);
        Vector3 direction1 = new Vector3(20.0f, 0.0f, 00.0f);

        Vector3 position2 = new Vector3(50.0f, 30.0f, 5.0f);
        Vector3 direction2 = new Vector3(20.0f, -90.0f, 00.0f);

        Vector3 position3 = new Vector3(-5.0f, 30.0f, 70.0f);
        Vector3 direction3 = new Vector3(20.0f, 180.0f, 00.0f);

        Vector3 position4 = new Vector3(-60.0f, 30.0f, 5.0f);
        Vector3 direction4 = new Vector3(20.0f, 90.0f, 00.0f);



        transform.position = position1;
        transform.rotation = Quaternion.Euler(direction1.x, direction1.y, direction1.z);

        //add positions to list
        camera_positions.Add(position1);
        camera_positions.Add(position2);
        camera_positions.Add(position3);
        camera_positions.Add(position4);
        //add rotations to list
        camera_rotations.Add(direction1);
        camera_rotations.Add(direction2);
        camera_rotations.Add(direction3);
        camera_rotations.Add(direction4);


    }


    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyUp("space"))
        {
            counter += 1;
            if (counter >= 4){
                counter = 0;
            }
            transform.position = camera_positions[counter];
            transform.rotation = Quaternion.Euler(camera_rotations[counter].x, camera_rotations[counter].y, camera_rotations[counter].z);
        }
    }






}
