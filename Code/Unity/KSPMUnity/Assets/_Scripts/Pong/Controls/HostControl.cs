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
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKey(KeyCode.W))
        {
            this.moving = true;
            this.displacement.Set(this.displacement.x, this.displacementFactor.y, this.displacement.z);
        }
        if (Input.GetKey(KeyCode.S))
        {
            this.moving = true;
            this.displacement.Set(this.displacement.x, -this.displacementFactor.y, this.displacement.z);
        }
	}

    void LateUpdate()
    {
        if (this.moving)
        {
            this.target.transform.position += this.displacement;
        }
        this.moving = false;
    }
}
