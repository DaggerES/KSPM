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
    }

    public static void SetLeftControls(ref UserHostControl userInput)
    {
        userInput.UpKey = KeyCode.W;
        userInput.DownKey = KeyCode.S;
        userInput.ResetBallKey = KeyCode.Space;
        userInput.displacementFactor.Set(0f, 0.1f, 0f);
    }

    public static void SetRightControls(ref UserHostControl userInput)
    {
        userInput.UpKey = KeyCode.UpArrow;
        userInput.DownKey = KeyCode.DownArrow;
        userInput.ResetBallKey = KeyCode.Space;
        userInput.displacementFactor.Set(0f, 0.1f, 0f);
    }

    public static void SetRemoteControls(ref UserHostControl userInput)
    {
        userInput.displacementFactor.Set(0f, 0.1f, 0f);
    }
}
