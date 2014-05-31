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

    void sceneManager_LoadingComplete(object sender, System.EventArgs e)
    {
        //Debug.Log(sender.ToString());
    }

    protected void LoadGame()
    {
        KSPMAction<object, object> action = kspmBridge.ActionsPool.BorrowAction;
        action.ActionKind = KSPMAction<object, object>.ActionType.EnumeratedMethod;
        action.ActionMethod.EnumeratedAction = this.sceneManager.LoadLevelAction;
        action.ParametersStack.Push("Game");
        action.ParametersStack.Push(this);
        this.kspmBridge.ActionsToDo.Enqueue(action);
    }
}
