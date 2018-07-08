using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLogic : MonoBehaviour
{

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Collider")
        {
            //ball hit a wall
        }
    }
}
