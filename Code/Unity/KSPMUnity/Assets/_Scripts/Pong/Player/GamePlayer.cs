using UnityEngine;
using System.Collections;

using KSPM.Network.Common;

public class GamePlayer : MonoBehaviour
{
    /// <summary>
    /// Cyclical reference to the NetworkEntity who is the container of this GamePlayer reference.
    /// </summary>
    public NetworkEntity Parent;

    /// <summary>
    /// Input handler to control the unity objects.
    /// </summary>
    public HostControl InputControl;

    /// <summary>
    /// What kind of rol this reference is going to be.
    /// </summary>
    public PlayerManager.GameRol GamingRol;

    /// <summary>
    /// Unique id assigned to this reference.<b>Do not confuse it with the NetworkEntity Id.</b>
    /// </summary>
    public int GameId;

    /// <summary>
    /// Flag to tell if this game player is local on this computer.
    /// </summary>
    protected bool localPlayer = false;

    /// <summary>
    /// Flag to tell if this GamePlayer is ready to play.
    /// </summary>
    public bool Ready = false;

    /// <summary>
    /// Sets the localPlayer flag to the given value.
    /// </summary>
    /// <param name="value"></param>
    public void SetLocal(bool value)
    {
        this.localPlayer = value;
    }

    /// <summary>
    /// Gets the localPlayer flag.
    /// </summary>
    public bool IsLocal
    {
        get
        {
            return this.localPlayer;
        }
    }

    /// <summary>
    /// Virtual method to release the GamePlayer.
    /// </summary>
    public virtual void Release()
    {
    }
}
