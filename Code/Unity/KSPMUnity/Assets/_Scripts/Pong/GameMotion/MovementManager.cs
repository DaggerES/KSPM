using UnityEngine;
using System.Collections;

public class MovementManager : MonoBehaviour
{
    public UnityGlobals.WorkingMode WorkingMode = UnityGlobals.WorkingMode.Client;

    public GameObject target;
    public bool moving;

    public Vector3 multiplicator;
    public Vector3 force;
    public Vector3 targetPosition;

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

        this.targetPosition.Set(this.target.transform.position.x, this.target.transform.position.y, this.target.transform.position.z);
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
        if (this.WorkingMode == UnityGlobals.WorkingMode.Server)
        {
            UnityGlobals.SingletonReference.KSPMServerReference.SendBallForce();
        }
    }

    public void ApplyForce(float x, float y, float z)
    {
        Debug.Log(x);
        this.force.Set(x, y, z);
        this.moving = true;
        if (this.WorkingMode == UnityGlobals.WorkingMode.Server)
        {
            UnityGlobals.SingletonReference.KSPMServerReference.SendBallForce();
        }
    }

    public GameError.ErrorType UpdateTargerPositionAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        float newX, newY, newZ;
        newZ = (float)parameters.Pop();
        newY = (float)parameters.Pop();
        newX = (float)parameters.Pop();
        this.target.transform.position.Set(newX, newY, newZ);
        Debug.Log(newX);
        return GameError.ErrorType.Ok;
    }

    public GameError.ErrorType ApplyForceToTargetAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        float newX, newY, newZ;
        newZ = (float)parameters.Pop();
        newY = (float)parameters.Pop();
        newX = (float)parameters.Pop();
        this.ApplyForce(newX, newY, newZ);
        return GameError.ErrorType.Ok;
    }
}
