using UnityEngine;
using System.Collections;

public class HostControl : MonoBehaviour
{

    public GameObject target;

    public Vector3 displacementFactor;

    public bool moving;

    protected Vector3 displacement;

	// Use this for initialization
	void Start ()
    {
        this.displacement = Vector3.zero;
        this.moving = false;
	}
}
