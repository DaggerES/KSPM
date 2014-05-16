using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    public SceneManager sceneManager;

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 128, 128), "Load"))
        {
            StartCoroutine(this.sceneManager.LoadLevel("Game"));
            StartCoroutine(PrepareScene());
        }
    }

    IEnumerator PrepareScene()
    {
        while (this.sceneManager.LoadingProgress < 1.0f)
        {
            yield return null;
        }
        Debug.Log("Complete");
    }
}
