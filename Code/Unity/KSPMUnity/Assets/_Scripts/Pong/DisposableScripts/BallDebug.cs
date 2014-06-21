using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class BallDebug : MonoBehaviour
{
    public GameObject target;

    void OnGUI()
    {
        GUI.TextField(new Rect(10, 120, 150, 20), target.transform.position.ToString());
    }
}
