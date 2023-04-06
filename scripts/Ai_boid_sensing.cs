using System;
using System.Collections.Generic;
using System.IO; // for debugging rays
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement; // for resetting scene on crash

public class Ai_boid_sensing : MonoBehaviour
{
    public SocketFloat SocketScript;
    public Check_for_collision CollisionScript;
    public Flock FlockScript;

    [Range(1, 100)]
    [SerializeField] private int _NumRows;
    public int NumRows { get { return _NumRows; } }

    [Range(1, 100)]
    [SerializeField] private int _NumCols;
    public int NumCols { get { return _NumCols; } }

    [Range(1, 100)]
    [SerializeField] private int _r_Value;
    public int r_Value { get { return _r_Value; } }

    [Header("Speed Variables")]
    [Range(0, 10)]
    [SerializeField]
    private float _MinSpeed;
    public float MinSpeed { get { return _MinSpeed; } }

    [Range(0, 10)]
    [SerializeField]
    private float _MaxSpeed;
    public float MaxSpeed { get { return _MaxSpeed; } }

    [SerializeField] private float SmoothDamp;

    private float speed = 1.5f;
    private Vector3 CurrentVelocity;

    public Transform MyTransform { get; set; }

    //list of the directions from the boid to raycast
    private List<Vector3> DirectionsToRaycast;

    //values returned from rays
    public List<float> DistancesToObstacles;

    [SerializeField] public float fitness;

    [SerializeField] public int steps_alive;

    [SerializeField] public int Num_boids_in_gen = 50;

    [SerializeField] private int Num_runs_per_boid = 2;
    //layer mask for the seperate rays (either obstacle or boids)
    [SerializeField] private LayerMask ObstacleMask;
    [SerializeField] private LayerMask OtherBoidsMask;
    [Header("Testing_mode")]
    [SerializeField]
    public int Testing_mode = 0;




    public List<Vector3> GetDirectionsToRaycast()
    {
        DirectionsToRaycast = new List<Vector3>(); // initialize the list

        //using polar coordinates to get the x,y,z values to raycast to.
        float x = 0;
        float y = 0;
        float z = 0;
        int step_row_value = 90 / NumRows;
        int step_col_value = 270 / NumCols;

        for (int phi = 45; phi >= -45; phi -= step_row_value)
        {
            y = Mathf.Sin(Mathf.PI / 180 * (phi));
            for (int theta = 225; theta >= -45; theta -= step_col_value)
            {
                x = Mathf.Cos(Mathf.PI / 180 * (theta));
                z = Mathf.Sin(Mathf.PI / 180 * (theta));
                DirectionsToRaycast.Add(new Vector3(r_Value * x, r_Value * y, r_Value * z));
            }
        }
        return DirectionsToRaycast;

    }

    //helper function for printing out arrays for debugging
    static void PrintPoints(List<float> List_of_floats_to_print, string Filename)
    {

        string path = "Assets/Resources/" + Filename + ".txt";
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        for (int i = 0; i < List_of_floats_to_print.Count; i++)
        {
            writer.WriteLine(List_of_floats_to_print[i]);
        }
        writer.Close();
        //Re-import the file to update the reference in the editor
        //only when editing in unity
        //AssetDatabase.ImportAsset(path);
        //TextAsset asset = (TextAsset)Resources.Load(Filename);

    }



    //move the boid (dictated by Direction_to_move)
    public void Moveboid(Vector3 Direction_to_move)
    {
        speed = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
        Direction_to_move = Vector3.SmoothDamp(MyTransform.forward, Direction_to_move, ref CurrentVelocity, SmoothDamp);
        Direction_to_move = Direction_to_move.normalized * speed;
        MyTransform.forward = Direction_to_move;
        MyTransform.position += Direction_to_move * Time.deltaTime;
    }



    public Vector3 Get_Average_position_From_Flock()
    {
        Vector3 Average_position = FlockScript.Get_Average_position();
        return Average_position;

    }

    public Vector3 Get_Average_direction_From_Flock()
    {
        Vector3 Average_direction = FlockScript.Get_Average_direction();
        return Average_direction;

    }


    public float Get_Fitness_Of_Distance()
    {
        float distance_fitness = 0;
        //variables for getting normal distribution of fitness based on distance from other boids
        double std = 3;
        double base_of_power;
        double the_power;
        double distance_to_aim_for = 5;

        //gets distance to the average center of the boids
        Vector3 Average_of_boids = Get_Average_position_From_Flock();
        float Distance_to_average = Vector3.Distance(Average_of_boids, MyTransform.position);


        base_of_power = 1 / (std * Math.Sqrt(2 * Math.PI)) * Math.E;

        the_power = Math.Exp(-0.5 * Math.Pow(((Distance_to_average - distance_to_aim_for) / std), 2));


        distance_fitness = ((float)(base_of_power * the_power)); ;
        return distance_fitness;
    }

    public List<float> GetRayData()
    {
        //empty DistancesToObstacles
        DistancesToObstacles.Clear();
        for (int i = 0; i < DirectionsToRaycast.Count; i++)
        {
            RaycastHit hit1;
            RaycastHit hit2;

            float obstacle_dist_value;
            float boid_dist_value;

            Vector3 Angles_to_rotate_rays = new Vector3 (MyTransform.localRotation.eulerAngles.x, MyTransform.localRotation.eulerAngles.y, MyTransform.localRotation.eulerAngles.z);

            //rotates rays when boid rotates.
            Vector3 Ray_with_rotation = RotateRay(DirectionsToRaycast[i], Angles_to_rotate_rays);



            //send out raycasts for obstacles and boids
            Physics.Raycast(MyTransform.position, Ray_with_rotation, hitInfo: out hit1, r_Value, ObstacleMask); // changed from DirectionsToRaycast[i] to Ray_with_rotation
            Physics.Raycast(MyTransform.position, Ray_with_rotation, hitInfo: out hit2, r_Value, OtherBoidsMask); // changed from DirectionsToRaycast[i] to Ray_with_rotation
            Debug.DrawRay(MyTransform.position, Ray_with_rotation,Color.red, r_Value);

            // so that 0 or null values arnt passed a value of the r_value + 10 is set if no object is detected.
            if (hit1.collider == null)
            {
                obstacle_dist_value = r_Value + 10;
            }
            else
            { //returns distance to collision if obstacle is hit by rau
                obstacle_dist_value = hit1.distance;
            }
            if (hit2.collider == null)
            {
                
                boid_dist_value = r_Value + 10;
            }
            else
            { // returns distance to collision if boids is hit by ray
                boid_dist_value = hit2.distance;
            }

            //add distance values to array
            DistancesToObstacles.Add(obstacle_dist_value);
            DistancesToObstacles.Add(boid_dist_value);

        }
        return DistancesToObstacles;
    }


    //function for rotating rays so they rotate with the boid 
    public Vector3 RotateRay(Vector3 point,Vector3 angles)
    {
        return Quaternion.Euler(angles) * point;
    }


    public void Awake()
    {
        fitness = 0;
        steps_alive = 0;
        MyTransform = transform;
        DirectionsToRaycast = GetDirectionsToRaycast();
        SocketScript = GetComponent<SocketFloat>();
        CollisionScript = GetComponent<Check_for_collision>();

        
        //setting up test mode
        if (Testing_mode == 1)
        {
            float[] Test_mode = new float[] { -4.0f, -4.0f }; // needs two bits of data to send
            SocketScript.SendData(Test_mode);

        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //increment time
        steps_alive += 1;
        fitness += 0.01f;
        float distance_fitness = Get_Fitness_Of_Distance();
        fitness += distance_fitness; // increase fitness by distnace to 
        float[] successful; // for debugging returned connection to socket
        float[] old_boid_data;

        //When in Testing mode output stats to files
        if (Testing_mode == 1)
        {
            if (CollisionScript.collided == true)
            {
                //restart scene
                SceneManager.LoadScene("SampleScene");
            }
            else
            {
                // used to gather data when testing

                //get average position of boids and print it out for stats
                //Vector3 Average_of_boids = Get_Average_position_From_Flock();
                //List<float> Average_position_of_boids = new List<float>();
                //Average_position_of_boids.Add(Average_of_boids.x);
                //Average_position_of_boids.Add(Average_of_boids.y);
                //Average_position_of_boids.Add(Average_of_boids.z);
                //PrintPoints(Average_position_of_boids, "Average_Position_of_boids");

                ////get average distance of AIBoid from boids and print it out for stats
                //float Distance_to_average = Vector3.Distance(Average_of_boids, MyTransform.position);
                //List<float> Dist_to_avg_in_list = new List<float>();
                //Dist_to_avg_in_list.Add(Distance_to_average);
                //PrintPoints(Dist_to_avg_in_list, "AverageDistance_to_boids");

                ////print average direction of boids
                //Vector3 Average_direction_of_boids = Get_Average_direction_From_Flock();
                //List<float> Average_direction_of_boids_in_list = new List<float>();
                //Average_direction_of_boids_in_list.Add(Average_direction_of_boids.x);
                //Average_direction_of_boids_in_list.Add(Average_direction_of_boids.y);
                //Average_direction_of_boids_in_list.Add(Average_direction_of_boids.z);
                //PrintPoints(Average_direction_of_boids_in_list, "AverageDirection_of_boids");
            }

        }


        if (Testing_mode == 0){
            //if boid has crashed and in Training mode
            if (CollisionScript.collided == true)
            {
                StaticAiBoidVars.num_crashes += 1;
                fitness -= 100;
                // if this boid has crashed 2 or more times then check if new generation or just new boid is needed
                if (StaticAiBoidVars.num_crashes >= Num_runs_per_boid)
                {
                    if (StaticAiBoidVars.boid_id >= Num_boids_in_gen)
                    {
                        Debug.Log($"---Generation {StaticAiBoidVars.generations} Finished ----");
                        //output generation over
                        old_boid_data = new float[] { -3.0f, fitness / Num_runs_per_boid, StaticAiBoidVars.boid_id, StaticAiBoidVars.generations };

                        successful = SocketScript.SendData(old_boid_data);
                        StaticAiBoidVars.generations += 1;
                        Debug.Log($"---Generation {StaticAiBoidVars.generations} Beginning ----");
                        StaticAiBoidVars.num_crashes = 0;
                        StaticAiBoidVars.boid_id = 0;
                        SceneManager.LoadScene("SampleScene");
                    }
                    else
                    {
                        //signal new boid
                        old_boid_data = new float[] { -2.0f, fitness / Num_runs_per_boid, StaticAiBoidVars.boid_id, StaticAiBoidVars.generations };
                        StaticAiBoidVars.boid_id += 1;
                        StaticAiBoidVars.num_crashes = 0;
                        successful = SocketScript.SendData(old_boid_data);
                        SceneManager.LoadScene("SampleScene");

                    }

                }
                SceneManager.LoadScene("SampleScene");

            }
            if (steps_alive >= 15000) // set a time that if surpassed by the boid, a new scene starts
            {
                StaticAiBoidVars.num_crashes += 1;
                steps_alive = 0;

                // if number of of crashes is greater than number of runs per boid, send signal to python to get new boid
                if (StaticAiBoidVars.num_crashes >= Num_runs_per_boid)
                {
                    if (StaticAiBoidVars.boid_id >= Num_boids_in_gen)
                    {
                        //signals new generation
                        old_boid_data = new float[] { -3.0f, fitness / Num_runs_per_boid, StaticAiBoidVars.boid_id, StaticAiBoidVars.generations };

                        successful = SocketScript.SendData(old_boid_data);
                        StaticAiBoidVars.generations += 1;
                        StaticAiBoidVars.num_crashes = 0;
                        StaticAiBoidVars.boid_id = 0;
                        //restarts scene
                        SceneManager.LoadScene("SampleScene");
                    }
                    else
                    {
                        //signals new boid
                        old_boid_data = new float[] { -2.0f, fitness / Num_runs_per_boid, StaticAiBoidVars.boid_id, StaticAiBoidVars.generations };
                        StaticAiBoidVars.boid_id += 1;
                        StaticAiBoidVars.num_crashes = 0;
                        //sends data
                        successful = SocketScript.SendData(old_boid_data);
                        //restarts scene
                        SceneManager.LoadScene("SampleScene");
                    }
                }
                else
                {
                    SceneManager.LoadScene("SampleScene");
                }

            }
        }

        //get data from rays  
        DistancesToObstacles = GetRayData();

        //before converting to array add flag to say that the incoming information is distances:
        DistancesToObstacles.Insert(0, -1.0f);
        //convert distancesToObstacles to an array
        float[] DistancesToObstacles_array = DistancesToObstacles.ToArray();
        //get the decision from the CNN
        float[] decision = SocketScript.SendData(DistancesToObstacles_array);
        //convert array to list so it can be printed
        List<float> decision_list = decision.ToList();
        Vector3 decision_vector = new Vector3(decision_list[0], decision_list[1], decision_list[2]);
        //print(decision_vector.ToString()); // for degbugging

        //move boids based on choice from CNN
        Moveboid(decision_vector);


        // Used to gather data when testing
        //if (Testing_mode == 1)
        //{
        //    List<float> Direction_of_AIBoid = new List<float>();
        //    Direction_of_AIBoid.Add(decision_vector.x);
        //    Direction_of_AIBoid.Add(decision_vector.y);
        //    Direction_of_AIBoid.Add(decision_vector.z);
        //    PrintPoints(Direction_of_AIBoid, "Direction_AIBoid");

        //    List<float> Position_of_AIboid = new List<float>();
        //    Position_of_AIboid.Add(MyTransform.position.x);
        //    Position_of_AIboid.Add(MyTransform.position.y);
        //    Position_of_AIboid.Add(MyTransform.position.z);
        //    PrintPoints(Position_of_AIboid, "Position_of_AIBoid");
        //}

    }
}

