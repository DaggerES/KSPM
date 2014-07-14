using UnityEngine;
using System.Collections;

public class VectorAttributeDebug : MonoBehaviour
{
    public HostControl target;

	// Use this for initialization
	void Start ()
    {
        target = null;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        if (this.target != null)
        {
            GUI.TextField(new Rect(this.transform.position.x, this.transform.position.y, 150f, 20f), this.target.Attribute().ToString());
        }
    }
}
