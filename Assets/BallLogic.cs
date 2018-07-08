using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLogic : MonoBehaviour
{

    private Vector3 direction = new Vector3(1f, .55f, .5f);
    public bool hitcolliderthisframe = false;
    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Collider" && hitcolliderthisframe == false)
        {
            hitcolliderthisframe = true;
            direction = Vector3.Reflect(direction, other.transform.forward.normalized);
            direction += new Vector3(UnityEngine.Random.Range(-.04f, .04f), UnityEngine.Random.Range(-.04f, .04f), UnityEngine.Random.Range(-.04f, .04f));

            if (direction.x > 1f || direction.y > 1f || direction.z > 1f || direction.x < -1f || direction.y < -1f || direction.z < -1f)
                direction = direction.normalized;

            transform.Translate(direction * Time.deltaTime * 3f);
        }
        else if (other.tag == "Slicer")
        {

        }
    }
    void Start()
    {

    }

    void FixedUpdate()
    {
        transform.Translate(direction.normalized * Time.fixedDeltaTime * 3f);

    }

    void LateUpdate()
    {
        hitcolliderthisframe = false;
    }
}
