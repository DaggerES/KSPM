using UnityEngine;
using System.Collections;

using KSPM.Network.Common;

public class GamePlayer : MonoBehaviour
{
    /// <summary>
    /// Cyclical reference to the NetworkEntity who is the container of this GamePlayer reference.
    /// </summary>
    public NetworkEntity Parent;
    public HostControl InputControl;

	// Use this for initialization
	void Start ()
    {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public virtual void Release()
    {
    }
}
