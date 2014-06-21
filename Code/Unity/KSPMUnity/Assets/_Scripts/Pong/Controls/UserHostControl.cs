using UnityEngine;
using System.Collections;

public class UserHostControl : HostControl 
{
    public KeyCode UpKey;
    public KeyCode DownKey;
    public KeyCode ResetBallKey;

    void Update()
    {
        if (Input.GetKey(UpKey))
        {
            this.moving = true;
            this.currentMovement = MovementAction.Up;
            this.displacement.Set(this.displacement.x, this.displacementFactor.y, this.displacement.z);
            UnityGlobals.SingletonReference.KSPMClientReference.SendControlsUpdate(MovementAction.Up);
        }
        if (Input.GetKey(DownKey))
        {
            this.moving = true;
            this.currentMovement = MovementAction.Down;
            this.displacement.Set(this.displacement.x, -this.displacementFactor.y, this.displacement.z);
            UnityGlobals.SingletonReference.KSPMClientReference.SendControlsUpdate(MovementAction.Down);
        }
        if (Input.GetKeyDown(ResetBallKey))
        {
            UnityGlobals.SingletonReference.KSPMClientReference.SendControlsUpdate(MovementAction.ResetBall);
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            UnityGlobals.SingletonReference.KSPMClientReference.gameManager.movementManager.target.transform.position = Vector3.zero;
        }
    }

    public static void SetLeftControls(UserHostControl userInput)
    {
        userInput.UpKey = KeyCode.W;
        userInput.DownKey = KeyCode.S;
        userInput.ResetBallKey = KeyCode.Space;
        userInput.displacementFactor.Set(0f, 0.1f, 0f);
    }

    public static void SetRightControls(UserHostControl userInput)
    {
        userInput.UpKey = KeyCode.UpArrow;
        userInput.DownKey = KeyCode.DownArrow;
        userInput.displacementFactor.Set(0f, 0.1f, 0f);
    }
}
