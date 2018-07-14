using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLogic : MonoBehaviour
{

    public Vector3 direction = new Vector3();
    public float speed;
    public bool hitcolliderthisframe = false;
    public ShliceMaster master;

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Collider" && hitcolliderthisframe == false)
        {
            hitcolliderthisframe = true;
            direction = Vector3.Reflect(direction, other.transform.forward.normalized);
            direction += new Vector3(UnityEngine.Random.Range(-.04f, .04f), UnityEngine.Random.Range(-.04f, .04f), UnityEngine.Random.Range(-.04f, .04f));

            if (direction.x > 1f || direction.y > 1f || direction.z > 1f || direction.x < -1f || direction.y < -1f || direction.z < -1f)
                direction = direction.normalized;

            transform.Translate(direction * Time.deltaTime * speed);
        }
        else if (other.tag == "SliceExpand")
        {
            master.CancelExpand();
        }
    }
    void Start()
    {
        master = GameObject.Find("MeshManager").GetComponent<ShliceMaster>();
    }

    void FixedUpdate()
    {
        transform.Translate(direction.normalized * Time.fixedDeltaTime * speed);

    }

    void LateUpdate()
    {
        hitcolliderthisframe = false;
    }
}
