using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class Drone : Enemy {

    GameManager gameManager;

    Rigidbody rb;

    Mothership _mothership;

    //Movement & Rotation Variables
    public float speed = 300f;
    private float rotationSpeed = 5.0f;
    private float adjRotSpeed;
    private Quaternion targetRotation;
    public GameObject target;
    public float targetRadius = 200f;

    //Boid Steering/Flocking Variables

    public float separationDistance = 25.0f;
    public float cohesionDistance = 50.0f;
    public float separationStrength = 250.0f;
    public float cohesionStrength = 25.0f;
    private Vector3 cohesionPos = new Vector3(0f, 0f, 0f);
    private int boidIndex = 0;

    //Drone Behaviour Variables
    public GameObject motherShip;
    public Vector3 scoutPosition;

    //Prey & Predator variables added
    private Vector3 tarVel;
    private Vector3 tarPrevPos;
    private Vector3 attackPos;
    private Vector3 fleePos;
    private float distanceRatio = 0.05f;
    
    //Drone Utility Variable
    private float attackOrFlee;

    // varibales added
    private float scoutTimer;
    private float detectTimer;
    private float scoutTime = 10.0f;
    private float detectTime = 5.0f;
    private float detectionRadius = 400.0f;
    private int newResourceVal;
    public GameObject newResourceObject;
    //Drone FSM Enumerator

    // set a bag for harvest resource
    public int resourceBag = 0;

    // set a int for normal forager keep harvest to the next resource object
    public int asteroidVisited = 0;

    public enum DroneBehaviours
    {
        Idle,
        Scouting,
        Elite_Foraging,
        Foraging,
        Attacking,
        Fleeing
    }

    public DroneBehaviours droneBehaviour;

    // Use this for initialization
    void Start() {

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        _mothership = GameObject.Find("Alien Mothership").GetComponent<Mothership>();

        rb = GetComponent<Rigidbody>();

        motherShip = gameManager.alienMothership;
        scoutPosition = motherShip.transform.position;
        
        // set the state to be idle 
        //droneBehaviour=DroneBehaviours.Idle;
    }

    // Update is called once per frame
    void Update() {

        //Acquire player if spawned in
        if (gameManager.gameStarted)
        {
            target = gameManager.playerDreadnaught;

            // Heuristic function here
            attackOrFlee = health * Friends();

            if (attackOrFlee >= 1000)
            {
                droneBehaviour = DroneBehaviours.Attacking;
            }
            else if (attackOrFlee < 1000) 
            {
                droneBehaviour = DroneBehaviours.Fleeing;
            }
            
        }

        //Boid cohesion/segregation
        BoidBehaviour();
    }

    //Calculate number of Friendly Units in targetRadius
    private int Friends()
    {
        int clusterStrength = 0;
        for (int i = 0; i < gameManager.enemyList.Length; i++)
        {
            if (Vector3.Distance(transform.position, gameManager.enemyList[i].transform.position) <
            targetRadius)
            {
                clusterStrength++;
            }
        }
        return clusterStrength;
    }
    private void MoveTowardsTarget(Vector3 targetPos)
    {
        //Rotate and move towards target if out of range
        if (Vector3.Distance(targetPos, transform.position) > targetRadius)
        {

            //Lerp Towards target
            targetRotation = Quaternion.LookRotation(targetPos - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);

            rb.AddRelativeForce(Vector3.forward * speed * 60 * Time.deltaTime);
        }
    }
    private void BoidBehaviour()
    {
        //Increment boid index reference
        boidIndex++;

        //Check if last boid in Enemy list
        if (boidIndex >= gameManager.enemyList.Length)
        {
            // Re - Compute the cohesionForce
            Vector3 cohesiveForce = (cohesionStrength / Vector3.Distance(cohesionPos, transform.position)) * (cohesionPos - transform.position);

            //Apply Force
            rb.AddForce(cohesiveForce);
            //Reset boidIndex
            boidIndex = 0;
            //Reset cohesion position
            cohesionPos.Set(0f, 0f, 0f);
        }

        //Currently analysed boid variables
        Vector3 pos = gameManager.enemyList[boidIndex].transform.position;
        Quaternion rot = gameManager.enemyList[boidIndex].transform.rotation;
        float dist = Vector3.Distance(transform.position, pos);

        if (dist > 0f)
        {
            //If within separation
            if (dist <= separationDistance)
            {
                //Compute scale of separation
                float scale = separationStrength / dist;
                //Apply force to ourselves
                rb.AddForce(scale * Vector3.Normalize(transform.position - pos));
            }
            //Otherwise if within cohesion distance of other boids
            else if (dist < cohesionDistance && dist > separationDistance)
            {
                //Calculate the current cohesionPos
                cohesionPos = cohesionPos + pos * (1f / (float)gameManager.enemyList.Length);
                //Rotate slightly towards current boid
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 1f);
            }
        }

        //Drone Behaviours - State Switching
        switch (droneBehaviour)
        {
            case DroneBehaviours.Idle:
                Idle();
                break;
            case DroneBehaviours.Elite_Foraging:
                Elite_Foraging();
                break;
            case DroneBehaviours.Foraging:
                //Foraging();
                break;
            case DroneBehaviours.Scouting:
                Scouting();
                break;
            case DroneBehaviours.Attacking:
                Attacking();
                break;
            case DroneBehaviours.Fleeing:
                Fleeing();
                break;

        }

    }
    //Drone FSM Behaviour - Scouting
    private void Scouting()
    {
        //If no new resource object found
        if (!newResourceObject)
        {
            //If close to scoutPosition, randomize new position to investigate within gamespace around mothership
            if (Vector3.Distance(transform.position, scoutPosition) < detectionRadius && Time.time > scoutTimer)
            {
                // Generate new random positionVector3 position;
                Vector3 position;
                position.x = motherShip.transform.position.x + Random.Range(-1500, 1500);
                position.y = motherShip.transform.position.y + Random.Range(-400, 400);
                position.z = motherShip.transform.position.z + Random.Range(-1500, 1500);

                scoutPosition = position;

                //Update scoutTimer
                scoutTimer = Time.time + scoutTime;
            }
            else
            {
                MoveTowardsTarget(scoutPosition);
                Debug.DrawLine(transform.position, scoutPosition, Color.yellow);
            }

            //Every few seconds, check for new resources
            if (Time.time > detectTimer)
            {
                newResourceObject = DetectNewResources();
                detectTimer = Time.time + detectTime;
            }
        }
        //Resource found, head back to Mothership
        else
        {
            // change the target to the mothership
            target = motherShip;
            // heding back to the mothership
            MoveTowardsTarget(target.transform.position);
            
            Debug.DrawLine(transform.position, target.transform.position, Color.green);

            //In range of mothership, relay information and reset to drone again
            if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
            {
                motherShip.GetComponent<Mothership>().drones.Add(this.gameObject);
                motherShip.GetComponent<Mothership>().scouts.Remove(this.gameObject);
                motherShip.GetComponent<Mothership>().resourceObjects.Add(newResourceObject);
                newResourceVal = 0;
                newResourceObject = null;
                droneBehaviour = DroneBehaviours.Idle;
            }
        }
    }
    private void Elite_Foraging() 
    {
        // Move to the resouce Asteroid
        MoveTowardsTarget(target.transform.position);

        Debug.DrawLine(transform.position, target.transform.position, Color.cyan);

        //If close to resouce Asteroid, randomize new position to investigate within gamespace around resouce Asteroid
        if (Vector3.Distance(transform.position, target.transform.position) < detectionRadius && Time.time > scoutTimer)
        {
            // Generate new random positionVector3 position;
            Vector3 position;
            position.x = motherShip.transform.position.x + Random.Range(-200, 200);
            position.y = motherShip.transform.position.y + Random.Range(-20, 20);
            position.z = motherShip.transform.position.z + Random.Range(-200, 200);

            target.transform.position = position;

            //Update scoutTimer
            scoutTimer = Time.time + scoutTime;

            newResourceObject = DetectNewResources();
        }

        // if not discover a better asteroid
        if (!newResourceObject)
        {
            Debug.DrawLine(transform.position, target.transform.position, Color.blue);
            // if the forager is located around the Asteroid
            if (Vector3.Distance(transform.position, target.transform.position) < targetRadius)
            {
                // check if the target is a Asteriod and it has resource 
                if (target.GetComponent<Asteroid>() != null && target.GetComponent<Asteroid>().resource > 0)
                {
                    resourceBag = target.GetComponent<Asteroid>().resource;
                    target.GetComponent<Asteroid>().resource = 0;
                }

                // if there is anything in the resource bag
                if (resourceBag != 0)
                {
                    //set the target to mothership
                    target = motherShip;

                    //head back to the mothership
                    MoveTowardsTarget(target.transform.position);

                    //if the forager is located around the mothership
                    if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
                    {
                        //mothership grab all the resource from the forager
                        motherShip.GetComponent<Mothership>().resourceFromForages += resourceBag;

                        //clear the resource bag of the forager
                        resourceBag = 0;

                        //add up the variable for going to the next resource object
                        asteroidVisited += 1;
                    }
                }
            }
            else
            {
                // Move to the resouce Asteroid
                MoveTowardsTarget(target.transform.position);
            }
        }
        // if a better asteroid is discovered
        // become an elite-scount keep the behavior so as not to overreach the mix scount count of 4
        else
        {
            if (newResourceObject.GetComponent<Asteroid>().resource > target.GetComponent<Asteroid>().resource)
            {
                // change the target to the mothership
                target = motherShip;
                // heding back to the mothership
                MoveTowardsTarget(target.transform.position);

                Debug.DrawLine(transform.position, target.transform.position, Color.magenta);

                //In range of mothership, relay information and reset to idle drone and await instruction
                if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
                {
                    motherShip.GetComponent<Mothership>().drones.Add(this.gameObject);
                    motherShip.GetComponent<Mothership>().elite_foragers.Remove(this.gameObject);
                    motherShip.GetComponent<Mothership>().resourceObjects.Add(newResourceObject);
                    newResourceVal = 0;
                    newResourceObject = null;
                    droneBehaviour = DroneBehaviours.Idle;
                }
            }
        }
    }
    private void Foraging() 
    {
        Debug.DrawLine(transform.position, target.transform.position, Color.cyan);
        
        // if there is anything in the resource bag
        if (resourceBag != 0)
        {
            //set the target to mothership
            target = motherShip;
            
            //head back to the mothership
            MoveTowardsTarget(target.transform.position);

            //if the forager is located around the mothership
            if (Vector3.Distance(transform.position, motherShip.transform.position) < targetRadius)
            {
                //mothership grab all the resource from the forager
                motherShip.GetComponent<Mothership>().resourceFromForages += resourceBag;

                //clear the resource bag of the forager
                resourceBag= 0;

                //add up the variable for going to the next resource object
                asteroidVisited += 1;
            }
        }
        // if there is nothing in the resource bag
        else 
        {
            // Move to the resouce Asteroid
            MoveTowardsTarget(target.transform.position);

            // if the forager is located around the Asteroid
            if (Vector3.Distance(transform.position, target.transform.position) < targetRadius)
            {
                // check if the target is a Asteriod and it has resource 
                if (target.GetComponent<Asteroid>() != null && target.GetComponent<Asteroid>().resource > 0)
                {
                    resourceBag = target.GetComponent<Asteroid>().resource;
                    target.GetComponent<Asteroid>().resource = 0;
                }
            }
        }

    }

    //keeps all the dones stay round the mothership
    private void Idle() 
    {
        MoveTowardsTarget(motherShip.transform.position);
    }

    //Drone FSM Behaviour - Attacking
    private void Attacking()
    {
        //Calculate target's velocity (without using RB)
        tarVel = (target.transform.position - tarPrevPos) / Time.deltaTime;
        tarPrevPos = target.transform.position;

        //Calculate intercept attack position (p = t + r * d * v)
        attackPos = target.transform.position + distanceRatio * Vector3.Distance(transform.position,
        target.transform.position) * tarVel;

        attackPos.y = attackPos.y + 10;
        Debug.DrawLine(transform.position, attackPos, Color.red);

        // Not in range of intercept vector - move into position
        if (Vector3.Distance(transform.position, attackPos) > targetRadius)
            MoveTowardsTarget(attackPos);
        else
        {
            //Look at target - Lerp Towards target
            targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);
            //Fire Weapons at target
            //...
            
        }
    }
    private void Fleeing()
    {
        //Calculate flee position
        fleePos = transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * rb.velocity;

        Debug.DrawLine(transform.position, fleePos, Color.white);

        //If not in range of flee position and drone is in the range of the player, move towards it
        if (Vector3.Distance(transform.position, fleePos) > targetRadius && Vector3.Distance(transform.position, target.transform.position) < targetRadius)
        {
            MoveTowardsTarget(fleePos);
        }
        // Head back to the MotherShip
        else
        {
            //Change the target to mothership
            target = motherShip;
            MoveTowardsTarget(target.transform.position);

            // check the distance between the drone and mothership if drones are around it resupply at the mothership.
            if (Vector3.Distance(transform.position, target.transform.position) < targetRadius)
            {
                droneBehaviour = DroneBehaviours.Idle;
            }
        }
    }
    private GameObject DetectNewResources()
    {
        //Go through list of asteroids and ...
        for (int i = 0; i < gameManager.asteroids.Length; i++)
        {
            //... check if they are within detection distance
            if (Vector3.Distance(transform.position, gameManager.asteroids[i].transform.position) <= detectionRadius)
            {
                //Find the best one
                if (gameManager.asteroids[i].GetComponent<Asteroid>().resource > newResourceVal)
                {
                    newResourceObject = gameManager.asteroids[i];
                }
            }
        }

        //Double check to see if the Mothership already knows about it and return it if not
        if (motherShip.GetComponent<Mothership>().resourceObjects.Contains(newResourceObject))
        {
            return null;
        }
        else
        {
            return newResourceObject;
        }

    }
}
