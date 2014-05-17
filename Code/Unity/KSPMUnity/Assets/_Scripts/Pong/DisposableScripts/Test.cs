using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    public SceneManager sceneManager;

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 128, 128), "Load"))
        {
            this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(sceneManager_LoadingComplete);
            //this.sceneManager.StartLoadingAsync(SceneManager.Scenes.Game);
            //this.sceneManager.LoadLevel(SceneManager.Scenes.Game);
            StartCoroutine(PrepareScene());
        }
    }

    public void ASD()
    {
        Application.LoadLevel("Game");
    }

    void sceneManager_LoadingComplete(object sender, System.EventArgs e)
    {
        Debug.Log(sender.ToString());
    }

    IEnumerator PrepareScene()
    {
        this.sceneManager.LoadLevel(SceneManager.Scenes.Game);
        while (this.sceneManager.LoadingProgress < 1.0f)
        {
            yield return null;
        }
        Debug.Log("Complete");
    }
}
