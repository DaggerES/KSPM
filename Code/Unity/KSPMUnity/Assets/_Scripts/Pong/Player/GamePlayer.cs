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

    public virtual void Release()
    {
    }
}
