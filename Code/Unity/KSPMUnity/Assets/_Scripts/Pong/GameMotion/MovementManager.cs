using UnityEngine;
using System.Collections;

public class MovementManager : MonoBehaviour
{
    public GameObject target;
    public bool moving;

    public Vector3 multiplicator;
    public Vector3 force;

	// Use this for initialization
	void Start ()
    {
        this.moving = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void FixedUpdate()
    {
        if (this.moving)
        {
            this.target.rigidbody.AddForce(this.force.x * this.multiplicator.x, this.force.y * this.multiplicator.y, this.force.z * this.multiplicator.z, ForceMode.Force);
            this.moving = false;
        }
    }

    public void RandomForce()
    {
        this.force.Set(Random.Range(-1.0f, .0f), Random.Range(-1.0f, 1.0f), 0);
        this.moving = true;
    }

    public void ApplyForce(float x, float y, float z)
    {
        this.force.Set(x, y, z);
        this.moving = true;
    }
}
