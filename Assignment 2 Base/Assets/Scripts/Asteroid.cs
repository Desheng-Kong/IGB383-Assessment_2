using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    // Start is called before the first frame update

    public int resource = 50;

    void Start()
    {
        resource = Random.Range(10, 100);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
