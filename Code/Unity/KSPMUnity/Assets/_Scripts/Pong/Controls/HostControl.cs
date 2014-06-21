using UnityEngine;
using System.Collections;

using KSPM.Network.Server;

public class HostControl : MonoBehaviour
{

    public GameObject target;

    public Vector3 displacementFactor;

    public bool moving;

    protected Vector3 displacement;

    public enum MovementAction : byte
    {
        None = 0,
        Up,
        Down,
        ResetBall,
    }

    public MovementAction currentMovement;

	// Use this for initialization
	void Start ()
    {
        this.displacement = Vector3.zero;
        this.moving = false;
        this.currentMovement = MovementAction.None;
	}

    void LateUpdate()
    {
        if (this.currentMovement != MovementAction.None)
        {
            this.target.transform.position += this.displacement;
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
}
