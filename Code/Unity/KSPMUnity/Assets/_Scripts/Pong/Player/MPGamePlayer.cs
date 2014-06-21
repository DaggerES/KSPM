using UnityEngine;
using System.Collections;

public class MPGamePlayer : GamePlayer
{
    public TickController tickController;

    public override void Release()
    {
        base.Release();
        this.tickController.Release();
    }
}
