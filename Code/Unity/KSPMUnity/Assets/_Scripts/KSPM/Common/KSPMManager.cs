using UnityEngine;
using System.Collections;

public class KSPMManager : MonoBehaviour
{
    public System.Collections.Generic.Queue<KSPMAction> ActionsToDo;

    public int poolSize = 32;

    public KSPMActionsPool ActionsPool;

    public SceneManager sceneManager;
    protected KSPMAction actionToDo;
	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this);
        this.ActionsPool = new KSPMActionsPool((uint)this.poolSize, new KSPMAction());
        this.ActionsToDo = new System.Collections.Generic.Queue<KSPMAction>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void LateUpdate()
    {
        if (this.ActionsToDo.Count > 0)
        {
            actionToDo = this.ActionsToDo.Dequeue();
            switch (actionToDo.ActionKind)
            {
                case KSPMAction.ActionType.EnumeratedMethod:
                    StartCoroutine(actionToDo.ActionMethod.EnumeratedAction(actionToDo.ParametersStack.Pop(), actionToDo.ParametersStack));
                    break;
                case KSPMAction.ActionType.NormalMethod:
                    Debug.Log("NormalMethod");
                    actionToDo.ActionMethod.BasicAction(actionToDo.ParametersStack.Pop(), actionToDo.ParametersStack);
                    break;
            }
            this.ActionsPool.Recyle(actionToDo);
        }
    }
}
