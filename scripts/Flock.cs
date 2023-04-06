using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class Flock : MonoBehaviour
{
    [Header("Spawn Variables")]
    [SerializeField]
    private IndvBoid IndvBoidPrefab;
    [SerializeField]
    private int flocksize;
    [SerializeField]
    private Vector3 SpawnBounds;

    [Header("Speed Variables")]
    [Range(0, 10)]
    [SerializeField]
    private float _MinSpeed;
    public float MinSpeed { get { return _MinSpeed; } }

    [Range(0, 10)]
    [SerializeField]
    private float _MaxSpeed;
    public float MaxSpeed { get { return _MaxSpeed; } }



    [Header("Detection Distances")]
    [Range(0,100)]
    [SerializeField]
    private float _CohesionDistance;
    public float CohesionDistance { get { return _CohesionDistance; } }

    [Range(0, 100)]
    [SerializeField]
    private float _AllignmentDistance;
    public float AllignmentDistance { get { return _AllignmentDistance; } }

    [Range(0, 100)]
    [SerializeField]
    private float _AvoidanceDistance;
    public float AvoidanceDistance { get { return _AvoidanceDistance; } }

    [Range(0, 100)]
    [SerializeField]
    private float _BoundsDistance;
    public float BoundsDistance { get { return _BoundsDistance; } }

    [Range(0, 100)]
    [SerializeField]
    private float _ObstacleAvoidanceDistance;
    public float ObstacleAvoidanceDistance { get { return _ObstacleAvoidanceDistance; } }

    [Header("Behaviour weights")]
    [Range(0, 10)]
    [SerializeField]
    private float _CohesionWeight;
    public float CohesionWeight { get { return _CohesionWeight; } }

    [Range(0, 10)]
    [SerializeField]
    private float _AllignmentWeight;
    public float AllignmentWeight { get { return _AllignmentWeight; } }

    [Range(0, 10)]
    [SerializeField]
    private float _AvoidanceWeight;
    public float AvoidanceWeight { get { return _AvoidanceWeight; } }

    [Range(0, 100)]
    [SerializeField]
    private float _BoundsWeight;
    public float BoundsWeight { get { return _BoundsWeight; } }

    [Range(0, 100)]
    [SerializeField]
    private float _ObstacleAvoidanceWeight;
    public float ObstacleAvoidanceWeight { get { return _ObstacleAvoidanceWeight; } }

    [Header("average position of flock")]
    public Vector3 Average_position;


    [Header("average direction of flock")]
    public Vector3 Average_direction;

    public IndvBoid[] AllUnits { get; set; }




    // Start is called before the first frame update
    void Start()
    {
        GenerateUnits();
    }


    public void GenerateUnits()
    {
        AllUnits = new IndvBoid[flocksize];
        for (int i = 0; i < flocksize; i++)
        {
            var RandomVector = Random.insideUnitSphere;
            RandomVector = new Vector3(RandomVector.x * SpawnBounds.x, RandomVector.y * SpawnBounds.y, RandomVector.z * SpawnBounds.y);
            var SpawnPosition = transform.position + RandomVector;
            var RandomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            AllUnits[i] = Instantiate(IndvBoidPrefab, SpawnPosition, RandomRotation);
            AllUnits[i].AssignFlock(this);
            AllUnits[i].InitialiseSpeed(Random.Range(MinSpeed, MaxSpeed)); 
        }


    }
    public Vector3 Get_Average_position()
    {
        Average_position = new Vector3(0, 0, 0);
        for (int i = 0; i < flocksize; i++)
        {
            Average_position += AllUnits[i].MyTransform.position;

        }
        Average_position /= flocksize;

        return Average_position;
    }


    public Vector3 Get_Average_direction()
    {
        Average_direction = new Vector3(0, 0, 0);
        for (int i = 0; i < flocksize; i++)
        {
            Average_direction += AllUnits[i].MoveVector_for_tests;

        }
        Average_direction /= flocksize;

        return Average_direction;
    }



    // Update is called once per frame
    void Update()
    {
        for (int i =0; i < AllUnits.Length; i++)
        {
            AllUnits[i].MoveUnit();

        }
        Get_Average_position();
    }
}
