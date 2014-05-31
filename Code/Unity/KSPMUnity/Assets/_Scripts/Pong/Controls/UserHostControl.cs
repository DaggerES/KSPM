using UnityEngine;
using System.Collections;

public class UserHostControl : HostControl 
{
    public KeyCode UpKey;
    public KeyCode DownKey;

    void Update()
    {
        if (Input.GetKey(UpKey))
        {
            this.moving = true;
            this.displacement.Set(this.displacement.x, this.displacementFactor.y, this.displacement.z);
            UnityGlobals.SingletonReference.KSPMClientReference.SendControlsUpdate(this.displacement);
        }
        if (Input.GetKey(DownKey))
        {
            this.moving = true;
            this.displacement.Set(this.displacement.x, -this.displacementFactor.y, this.displacement.z);
            UnityGlobals.SingletonReference.KSPMClientReference.SendControlsUpdate(this.displacement);
        }
    }

    void LateUpdate()
    {
        if (this.moving)
        {
            this.target.transform.position += this.displacement;
            this.moving = false;
        }
    }

    public static void SetLeftControls(UserHostControl userInput)
    {
        userInput.UpKey = KeyCode.W;
        userInput.DownKey = KeyCode.S;
        userInput.displacementFactor.Set(0f, 0.1f, 0f);
    }

    public static void SetRightControls(UserHostControl userInput)
    {
        userInput.UpKey = KeyCode.UpArrow;
        userInput.DownKey = KeyCode.DownArrow;
    }
}
