using UnityEngine;
using System.Collections;

using KSPM.Network.Server;

public class HostControl : MonoBehaviour, IPersistentAttribute<Vector3>
{
    public GamePlayer Owner;

    public GameObject target;

    public Vector3 displacementFactor;

    public bool moving;

    protected Vector3 displacement;

    public Vector3 PersistentPosition;

    public enum MovementAction : byte
    {
        None = 0,
        Up,
        Down,
        ResetBall,
        RemoteControl,
    }

    /// <summary>
    /// Current action to do.
    /// </summary>
    public MovementAction currentMovement;

    /// <summary>
    /// Last action performed by this control.
    /// </summary>
    public MovementAction lastMovementAction;

	// Use this for initialization
	void Start ()
    {
        this.displacement = Vector3.zero;
        this.moving = false;
        this.currentMovement = MovementAction.None;
	}

    public void SetDebug()
    {
        VectorAttributeDebug debugger;
        debugger = this.gameObject.AddComponent<VectorAttributeDebug>();
        debugger.target = this;
        Debug.Log(debugger.target);
    }

    void LateUpdate()
    {
        if (this.currentMovement != MovementAction.None)
        {
            if (this.currentMovement == MovementAction.RemoteControl)
            {
                this.target.transform.position = this.PersistentPosition;
                //this.target.transform.position.Set(this.PersistentPosition.x, this.PersistentPosition.y, this.PersistentPosition.z);
                //Debug.Log("RPP: " + this.target.transform.position);
            }
            else
            {
                this.target.transform.position += this.displacement;
                this.PersistentPosition.Set(this.target.transform.position.x, this.target.transform.position.y, this.target.transform.position.z);
            }
            this.moving = false;
            this.currentMovement = MovementAction.None;
        }
    }

    public static GameError.ErrorType StaticMoveTargetAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        ServerSideClient ssClientReference = (ServerSideClient)caller;
        HostControl.MovementAction actionToDo = (MovementAction)parameters.Pop();
        switch (actionToDo)
        {
            case MovementAction.Up:
                ((GameObject)(ssClientReference.gameUser.UserDefinedHolder)).GetComponent<MPGamePlayer>().InputControl.displacement.Set(0f, 0.1f, 0f);
                ((GameObject)(ssClientReference.gameUser.UserDefinedHolder)).GetComponent<MPGamePlayer>().InputControl.currentMovement = actionToDo;
                break;
            case MovementAction.Down:
                ((GameObject)(ssClientReference.gameUser.UserDefinedHolder)).GetComponent<MPGamePlayer>().InputControl.displacement.Set(0f, -0.1f, 0f);
                ((GameObject)(ssClientReference.gameUser.UserDefinedHolder)).GetComponent<MPGamePlayer>().InputControl.currentMovement = actionToDo;
                break;
            case MovementAction.ResetBall:
                UnityGlobals.SingletonReference.KSPMServerReference.gameManager.currentStatus = GameManager.GameStatus.Playing;
                UnityGlobals.SingletonReference.KSPMServerReference.gameManager.movementManager.RandomForce();
                break;
            case MovementAction.None:
                ((GameObject)(ssClientReference.gameUser.UserDefinedHolder)).GetComponent<MPGamePlayer>().InputControl.displacement.Set(0f, 0f, 0f);
                ((GameObject)(ssClientReference.gameUser.UserDefinedHolder)).GetComponent<MPGamePlayer>().InputControl.currentMovement = actionToDo;
                break;
        }
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }


    public void SetPersistent(Vector3 value)
    {
    }

    public void SetPersistentRef(ref Vector3 value)
    {
    }

    public void UpdatePersistentValue(Vector3 value)
    {
        //Debug.Log(this.Owner.IsLocal);
        if (!this.Owner.IsLocal)
        {
            this.PersistentPosition.Set(value.x, value.y, value.z);
            this.currentMovement = MovementAction.RemoteControl;
        }
    }

    public Vector3 Attribute()
    {
        //Debug.Log("PP: " + this.PersistentPosition);
        return this.PersistentPosition;
    }
}
