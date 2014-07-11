using UnityEngine;
using System.Collections;

public class MovementManager : MonoBehaviour, IPersistentAttribute<Vector3>
{
    public UnityGlobals.WorkingMode WorkingMode = UnityGlobals.WorkingMode.Client;

    public GameObject target;
    protected Rigidbody physicsTarget;
    public bool IsPhysicsTarget;
    public bool moving;

    public Vector3 multiplicator;
    public Vector3 force;
    public Vector3 targetPosition;

	// Use this for initialization
	void Start ()
    {
        this.moving = false;
        this.physicsTarget = target.rigidbody;
	}

    void FixedUpdate()
    {
        if (this.IsPhysicsTarget)
        {
            this.targetPosition.Set(this.physicsTarget.position.x, this.physicsTarget.position.y, this.physicsTarget.position.z);
        }
        else
        {
            this.targetPosition.Set(this.target.transform.position.x, this.target.transform.position.y, this.target.transform.position.z);
        }
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
        this.force.Set(x, y, z);
        this.moving = true;
        if (this.WorkingMode == UnityGlobals.WorkingMode.Server)
        {
            //UnityGlobals.SingletonReference.KSPMServerReference.SendBallForce();
        }
    }

    public GameError.ErrorType UpdateTargetPositionAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        float newX, newY, newZ;
        newZ = (float)parameters.Pop();
        newY = (float)parameters.Pop();
        newX = (float)parameters.Pop();
        if (this.IsPhysicsTarget)
        {
            this.physicsTarget.MovePosition(new Vector3(newX, newY, newZ));
        }
        else
        {
            this.target.transform.position.Set(newX, newY, newZ);
        }
        //Debug.Log(this.target.transform.position);
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

    public void SetPersistent(Vector3 value)
    {
    }

    public void SetPersistentRef(ref Vector3 value)
    {
    }

    public void UpdatePersistentValue(Vector3 value)
    {
        if (this.IsPhysicsTarget)
        {
            this.physicsTarget.MovePosition(new Vector3(value.x, value.y, value.z));
        }
        else
        {
            this.target.transform.position.Set(value.x, value.y, value.z);
        }
    }

    public Vector3 Attribute()
    {
        return this.targetPosition;
    }
}
