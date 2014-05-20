using UnityEngine;
using System.Collections;

public class KSPMManager : MonoBehaviour
{
    public System.Collections.Generic.Queue<KSPMAction> ActionsToDo;

    public SceneManager sceneManager;
    public bool loadGameScene;
    protected KSPMAction actionToDo;
	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this);
        this.ActionsToDo = new System.Collections.Generic.Queue<KSPMAction>();
        this.sceneManager.LoadingComplete += new SceneManager.LoadingCompleteEventHandler(this.sceneManager_LoadingComplete);
	}

    void sceneManager_LoadingComplete(object sender, System.EventArgs e)
    {
        Debug.Log("HOLA");
        Debug.Log(sender.ToString());
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void LateUpdate()
    {
        if (this.ActionsToDo.Count > 0)
        {
            actionToDo = this.ActionsToDo.Dequeue();
            StartCoroutine(actionToDo.method.IEnumerateActionMethod(null, actionToDo.actionParameter));
            //StartCoroutine(actionToDo.IEnumerateActionMethod(null, actionToDo.actionParameter));
        }
    }

    public void load()
    {
        //StartCoroutine(this.sceneManager.LoadLevel("Game"));
    }
}
