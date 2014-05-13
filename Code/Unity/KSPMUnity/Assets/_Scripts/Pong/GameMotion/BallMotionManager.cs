using UnityEngine;
using System.Collections;

public class BallMotionManager : MonoBehaviour
{
    public float maxVelocity;
    public bool speeding;
	// Use this for initialization
	void Start ()
    {
        this.speeding = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void FixedUpdate()
    {
        if (this.rigidbody.velocity.magnitude > this.maxVelocity)
        {
            //this.rigidbody.velocity.
        }
    }
}
