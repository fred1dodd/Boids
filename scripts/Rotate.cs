using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] private Vector3 _rotation;

    // Update is called once per frame
    void Update()
    {
        //used for rotating wind turbine
        transform.Rotate(_rotation * Time.deltaTime);
    }
}
