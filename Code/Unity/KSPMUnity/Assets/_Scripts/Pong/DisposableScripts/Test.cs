using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    public SceneManager sceneManager;
    public KSPMManager kspmBridge;

    void Start()
    {
        this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(sceneManager_LoadingComplete);
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 128, 128), "Load"))
        {
            //this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(sceneManager_LoadingComplete);
            //this.sceneManager.StartLoadingAsync(SceneManager.Scenes.Game);
            //this.sceneManager.LoadLevel(SceneManager.Scenes.Game);
            LoadGame();
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

    protected void LoadGame()
    {
        KSPMAction action = new KSPMAction(KSPMAction.ActionType.LoadScene, "Game");
        action.method.IEnumerateActionMethod = this.sceneManager.LoadLevel;
        this.kspmBridge.ActionsToDo.Enqueue(action);
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
