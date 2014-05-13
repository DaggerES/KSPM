using UnityEngine;
using System.Collections;

public class BarCollitionManager : MonoBehaviour
{

    public MovementManager movementManager;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        movementManager.ApplyForce(collision.relativeVelocity.x * -0.12f, collision.relativeVelocity.y * 0.12f * ( this.gameObject.transform.position.x * -0.8f ), collision.relativeVelocity.z * 0.12f);
        //movementManager.ApplyForce(collision.rigidbody.velocity.x, collision.rigidbody.velocity.y * -2, collision.rigidbody.velocity.z);
    }
}
