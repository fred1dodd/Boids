using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Check_for_collision : MonoBehaviour
{
    [SerializeField] // just for debugging
    public bool collided;
    public void Awake()
    {
        collided = false;
    }
    public int OnTriggerEnter(Collider Other)
    {
        // just check if gameobject tag is AI__boid

        if (Other.gameObject.tag == "AI_boid")
        {
            Debug.Log("Boid crashed into ground/wall");

            collided = true;
            return 1;
        }
        else
        {
            collided = true;
            return 0;
        }

    }

}


