using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

public class Mothership : MonoBehaviour {

    public GameObject enemy;
    public int numberOfEnemies = 20;

    public GameObject spawnLocation;
    public List<GameObject> resourceObjects = new List<GameObject>();
    //Resource Harvesting Variables
    public List<GameObject> drones = new List<GameObject>();
    public List<GameObject> scouts = new List<GameObject>();

    // set the forager list
    public List<GameObject> foragers = new List<GameObject>();
    public List<GameObject> elite_foragers=new List<GameObject>();

    public int maxScouts = 4;

    // set the count for the foragers list
    public int maxForagers = 3;
    public int maxEliteForagers = 2;


    // set the array for normal foragers keep harvesting
    public int[] targetForNormalForager = { 0, 0, 0 };

    // set the array for elite foragers keep harvesting
    public int[] targetForEliteForager = { 0, 0 };

    private float forageTimer;
    private float forageTime = 10.0f;
    
    // this the the resouce that get from all the foragers
    public int resourceFromForages = 0;
    // the needs for mothership to wrap away
    public int resourceNeedForWrapAway = 3000;
    public bool WrapAway = false;
    // initialise the boids
    void Start() {

        for (int i = 0; i < numberOfEnemies; i++) {

            Vector3 spawnPosition = spawnLocation.transform.position;

            spawnPosition.x = spawnPosition.x + Random.Range(-50, 50);
            spawnPosition.y = spawnPosition.y + Random.Range(-50, 50);
            spawnPosition.z = spawnPosition.z + Random.Range(-50, 50);

            GameObject thisEnemy = Instantiate(enemy, spawnPosition, spawnLocation.transform.rotation);
            drones.Add(thisEnemy);
        }
    }



    // Update is called once per frame
    void Update() 
    {
        // whenever the resource reach the needs
        if (resourceFromForages >= resourceNeedForWrapAway) 
        {
            // the Alien Mothership can warp away
            WrapAway = true;
        }
        
        // remove the dead drones and reduce the size of the list
        for (int i = 0; i < drones.Count; i++) 
        {
            if (drones[i] == null) 
            {
                drones.Remove(drones[i]);
                drones.Capacity -= 1;
            }
        }

        // Sort the drones with the fitness
        drones.Sort(delegate (GameObject a, GameObject b)
        {   
            // if the set drones not equal to null
            if(a != null && b != null) 
            {
                return (b.GetComponent<Drone>().health).CompareTo(a.GetComponent<Drone>().health);
            }
            return 0;
        });

        //(Re)Initialise Scouts Continuously
        if (scouts.Count < maxScouts)
        {
            scouts.Add(drones[0]);
            drones.Remove(drones[0]);
            // always the last one 
            scouts[scouts.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Scouting;
        }

        //(Re)Determine best resource objects periodically
        if (resourceObjects.Count > 0 && Time.time > forageTimer)
        {
            //Sort resource objects delegated by their resource amount in decreasing order
            resourceObjects.Sort(delegate (GameObject a, GameObject b) 
            {
                return(b.GetComponent<Asteroid>().resource).CompareTo(a.GetComponent<Asteroid>().resource);
            });
         
            forageTimer = Time.time + forageTime;
        }

        // if there are at least 5 resoure been populated
        if (resourceObjects.Count >= 5)
        {
          
            // Asign two elite foragers
            if (elite_foragers.Count < maxEliteForagers)
            {
                //add the fitness NO.1 drons from the done list;
                elite_foragers.Add(drones[0]);
                drones.Remove(drones[0]);
                //(Re)Initialise foragers Continuously
                elite_foragers[elite_foragers.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Elite_Foraging;
                // Initialise foragers target = target will be 1st 2nd resource obeject
                elite_foragers[elite_foragers.Count - 1].GetComponent<Drone>().target = resourceObjects[elite_foragers.Count - 1];
            }
            else 
            {
                //keep updating "CURRENT" 1st, 2nd target to those elite foragers

                for (int i = 0; i < elite_foragers.Count; i++)
                {
                    // if any of the elite foragers finished harvesting the current target asteroid
                    if (elite_foragers[i].GetComponent<Drone>().asteroidVisited > targetForEliteForager[i])
                    {
                        // change the target to the newest 1st,2nd asteroid in the resource list
                        elite_foragers[i].GetComponent<Drone>().target = resourceObjects[i];
                        // add the taget index ready for the next check 
                        targetForEliteForager[i]++;
                    }
                }

                /*
                //keep updating the first 2 target to those elite foragers
                elite_foragers[0].GetComponent<Drone>().target = resourceObjects[0];
                elite_foragers[1].GetComponent<Drone>().target = resourceObjects[1];
                */
            }

            // Asign three noraml foragers
            if (foragers.Count < maxForagers)
            {
                //add the fitness NO.1 drons from the done list;
                foragers.Add(drones[0]);
                drones.Remove(drones[0]);
                //(Re)Initialise foragers Continuously
                foragers[foragers.Count - 1].GetComponent<Drone>().droneBehaviour = Drone.DroneBehaviours.Foraging;
                // Initialise foragers target = target will be 3rd 4th 5th resource obeject
                foragers[foragers.Count - 1].GetComponent<Drone>().target = resourceObjects[foragers.Count + 1];

            }
            else 
            {
                //keep updating "CURRENT" 3rd,4th,5th, target to those normal foragers

                for (int i = 0; i < foragers.Count; i++)
                {
                    // if any of the normal foragers finished harvesting the current target asteroid
                    if (foragers[i].GetComponent<Drone>().asteroidVisited > targetForNormalForager[i])
                    {
                        // change the target to the newest 3rd 4th 5th asteroid in the resource list
                        foragers[i].GetComponent<Drone>().target = resourceObjects[i + 2];
                        // add the taget index ready for the next check 
                        targetForNormalForager[i]++;
                    }
                }
            }
        }
    }
}

