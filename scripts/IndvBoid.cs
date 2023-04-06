using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndvBoid : MonoBehaviour
{
    [SerializeField] private float FOVAngle;
    [SerializeField] private float SmoothDamp; // lower this value is the closer to cohesion vector we get in a frame
    [SerializeField] private LayerMask ObstacleMask; // Obstacle mask to avoid
    [SerializeField] private Vector3[] DirectionsToCheckToAvoidObstacle; // array of vectors to check for best obstacle avoidance

    private List<IndvBoid> CohesionNeighbours = new List<IndvBoid>();
    private List<IndvBoid> AvoidanceNeighbours = new List<IndvBoid>();
    private List<IndvBoid> AllignmentNeighbours = new List<IndvBoid>();
    private Flock AssignedFlock;
    private Vector3 CurrentVelocity;
    private Vector3 CurrentObstacleAvoidanceVector;
    private float speed;

    public Vector3 MoveVector_for_tests;

    public Transform MyTransform { get; set; }

    public void Awake()
    {
        MyTransform = transform;
    }

    public void AssignFlock(Flock flock)
    {
        AssignedFlock = flock;
    }

    public void InitialiseSpeed(float speed)
    {
        this.speed = speed;
    }


    public void MoveUnit()
    {
        //gets neighbours
        FindNeighbours();
        //calculates speed based on surrounding boids
        CalculateSpeed();

        //get movement vectors + obstacle avoidance vector for best avoidance of obstacles
        var CohesionVector = CalculateCohesionVector() * AssignedFlock.CohesionWeight;
        var AvoidanceVector = CalculateAvoidanceVector() * AssignedFlock.AvoidanceWeight;
        var AllignmentVector = CalculateAllignmentVector() * AssignedFlock.AllignmentWeight;
        var ObstacleAvoidanceVector = CalculateObstacleAvoidanceVector() * AssignedFlock.ObstacleAvoidanceWeight;

        //combines vectors into single MoveVector
        var MoveVector = CohesionVector + AllignmentVector + AvoidanceVector  + ObstacleAvoidanceVector; 
        MoveVector = Vector3.SmoothDamp(MyTransform.forward, MoveVector, ref CurrentVelocity, SmoothDamp);
        MoveVector = MoveVector.normalized * speed;
        MoveVector_for_tests = MoveVector; // for testing

        //move boid
        MyTransform.forward = MoveVector;
        MyTransform.position += MoveVector * Time.deltaTime;
    }


    private void CalculateSpeed()
    {
        if (CohesionNeighbours.Count == 0)
        {
            return;
        }
        speed = 0;
        for (int i = 0; i < CohesionNeighbours.Count; i++)
        {
            speed += CohesionNeighbours[i].speed;
        }
        speed /= CohesionNeighbours.Count;
        speed = Mathf.Clamp(speed, AssignedFlock.MinSpeed, AssignedFlock.MaxSpeed);
    }

    private void FindNeighbours()
    {
        CohesionNeighbours.Clear();
        AllignmentNeighbours.Clear();
        AvoidanceNeighbours.Clear();

        var AllUnits = AssignedFlock.AllUnits;

        for (int i = 0; i < AllUnits.Length; i++)
        {
            var CurrentUnit = AllUnits[i];
            if (CurrentUnit != this)
            {
                float CurrentNeightbourDistanceSqr = Vector3.SqrMagnitude(CurrentUnit.MyTransform.position - MyTransform.position);
                if (CurrentNeightbourDistanceSqr <= AssignedFlock.CohesionDistance * AssignedFlock.CohesionDistance)
                {
                    CohesionNeighbours.Add(CurrentUnit);
                }
                if (CurrentNeightbourDistanceSqr <= AssignedFlock.AllignmentDistance * AssignedFlock.AllignmentDistance)
                {
                    AllignmentNeighbours.Add(CurrentUnit);
                }
                if (CurrentNeightbourDistanceSqr <= AssignedFlock.AvoidanceDistance * AssignedFlock.AvoidanceDistance)
                {
                    AvoidanceNeighbours.Add(CurrentUnit);
                }


            }

        }


    }

    private Vector3 CalculateCohesionVector()
    {
        var CohesionVector = Vector3.zero;
        
        if (CohesionNeighbours.Count == 0)
        {
            return CohesionVector; // returns vector3.zero when no cohesion neighbours
        }
        int NeighboursInFOV = 0;
        for (int i = 0; i < CohesionNeighbours.Count; i++)
        {
            if (IsInFOV(CohesionNeighbours[i].MyTransform.position))
            {
                NeighboursInFOV++;
                CohesionVector += CohesionNeighbours[i].MyTransform.position;

            }
        }
        CohesionVector /= NeighboursInFOV;
        CohesionVector -= MyTransform.position;
        CohesionVector = CohesionVector.normalized;
        return CohesionVector;


    }

    //gets alignment vector
    private Vector3 CalculateAllignmentVector()
    {
        var AllignmentVector = MyTransform.forward;
        if (AllignmentNeighbours.Count == 0)
        {
            return AllignmentVector;
        }
        int NeighboursInFOV = 0;
        for (int i = 0; i < AllignmentNeighbours.Count; i++)
        {
            if (IsInFOV(AllignmentNeighbours[i].MyTransform.position))
            {
                NeighboursInFOV++;
                AllignmentVector += AllignmentNeighbours[i].MyTransform.forward;

            }
        }
        if (NeighboursInFOV == 0)
        {
            return MyTransform.forward;
        }

        AllignmentVector /= NeighboursInFOV;
        AllignmentVector = AllignmentVector.normalized;
        return AllignmentVector;
    }

    //gets avoidance vetor
    private Vector3 CalculateAvoidanceVector()
    {
        var AvoidanceVector = Vector3.zero;
        if (AvoidanceNeighbours.Count == 0)
        {
            return AvoidanceVector;
        }
        int NeighboursInFOV = 0;
        for (int i = 0; i < AvoidanceNeighbours.Count; i++)
        {
            if (IsInFOV(AvoidanceNeighbours[i].MyTransform.position))
            {
                NeighboursInFOV++;
                AvoidanceVector += (MyTransform.position - AvoidanceNeighbours[i].MyTransform.position);

            }
        }
        if (NeighboursInFOV == 0)
        {
            return Vector3.zero;
        }
        AvoidanceVector /= NeighboursInFOV;
        AvoidanceVector = AvoidanceVector.normalized;
        return AvoidanceVector;
    }


    //calculate the best path to take to avoid obstacles
    private Vector3 CalculateObstacleAvoidanceVector()
    {
        var ObstacleAvoidanceVector = Vector3.zero;

        RaycastHit hit;

        if (Physics.Raycast(MyTransform.position, MyTransform.forward, out hit, AssignedFlock.ObstacleAvoidanceDistance, ObstacleMask))
        {

            ObstacleAvoidanceVector = FindBestDirectionToAvoidObstacle();
        }
        else
        {
            CurrentObstacleAvoidanceVector = Vector3.zero; // stores current vector when no objects found
        }

        return ObstacleAvoidanceVector;

    }


    //find the best direction from set of given vectors to avoid obstacles
    private Vector3 FindBestDirectionToAvoidObstacle()
    {
        //if current OAV isnt vector3.zero check that it still doesnt detect an object
        if (CurrentObstacleAvoidanceVector != Vector3.zero)
        {
            RaycastHit hit;
            if(Physics.Raycast(MyTransform.position,MyTransform.forward,out hit, AssignedFlock.ObstacleAvoidanceDistance,ObstacleMask))
            {
                return CurrentObstacleAvoidanceVector;
            }

        }
        float maxDistance = int.MinValue;
        var SelectedDirection = Vector3.zero;
        for (int i = 0; i< DirectionsToCheckToAvoidObstacle.Length; i++)
        {
            RaycastHit hit;
            var CurrentDirection = MyTransform.TransformDirection(DirectionsToCheckToAvoidObstacle[i].normalized);
            if(Physics.Raycast(MyTransform.position,CurrentDirection,out hit,AssignedFlock.ObstacleAvoidanceDistance,ObstacleMask))
            {
                float CurrentDistance = (hit.point - MyTransform.position).sqrMagnitude;
                if (CurrentDistance > maxDistance)
                {
                    maxDistance = CurrentDistance;
                    SelectedDirection = CurrentDirection;

                }
            }
            else
            {
                SelectedDirection = CurrentDirection;
                CurrentObstacleAvoidanceVector = CurrentDirection.normalized;
                return SelectedDirection.normalized;

            }
        }
        return SelectedDirection.normalized;
    }


    //returns true if angle between forward direction of boid1  and the angle to boid2 is less than the FOV angle of boid 1
    private bool IsInFOV(Vector3 position)
    {
        return Vector3.Angle(MyTransform.forward, position - MyTransform.forward) <= FOVAngle;
    } 


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
