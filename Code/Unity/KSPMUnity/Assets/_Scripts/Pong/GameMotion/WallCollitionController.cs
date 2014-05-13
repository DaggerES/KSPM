using UnityEngine;
using System.Collections;

public class WallCollitionController : MonoBehaviour
{
    public MovementManager movementManager;
    public Vector3 collitionForceFactor;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.relativeVelocity.y);
        movementManager.ApplyForce(collision.relativeVelocity.x * this.collitionForceFactor.x, collision.relativeVelocity.y * this.collitionForceFactor.y, collision.relativeVelocity.z * this.collitionForceFactor.z);
        //movementManager.ApplyForce(collision.rigidbody.velocity.x, collision.rigidbody.velocity.y * -2, collision.rigidbody.velocity.z);
    }
}
