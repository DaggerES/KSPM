using UnityEngine;
using System.Collections;

public class KSPMManager : MonoBehaviour
{
    public System.Collections.Generic.Queue<KSPMAction<object, object>> ActionsToDo;    

    public KSPMActionsPool<object, object> ActionsPool;

    public int poolSize = 32;

    public SceneManager sceneManager;
    protected KSPMAction<object, object> currentAction;

	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this);
        this.ActionsPool = new KSPMActionsPool<object, object>((uint)this.poolSize, new KSPMAction<object, object>());
        this.ActionsToDo = new System.Collections.Generic.Queue<KSPMAction<object, object>>();
	}

    void FixedUpdate()
    {
        object returnedParameter;
        object caller;
        if (this.ActionsToDo.Count > 0)
        {
            this.currentAction = this.ActionsToDo.Dequeue();
            switch (this.currentAction.ActionKind)
            {
                case KSPMAction<object, object>.ActionType.EnumeratedMethod:
                    caller = this.currentAction.ParametersStack.Pop();
                    StartCoroutine(this.currentAction.ActionMethod.EnumeratedAction(caller, this.currentAction.ParametersStack));
                    break;
                case KSPMAction<object, object>.ActionType.NormalMethod:
                    {
                        caller = this.currentAction.ParametersStack.Pop();
                        returnedParameter = this.currentAction.ActionMethod.BasicAction(caller, this.currentAction.ParametersStack);
                        this.currentAction.ParametersStack.Push(returnedParameter);
                        this.currentAction.OnActionCompleted(caller, this.currentAction.ParametersStack);
                        break;
                    }
            }
            this.ActionsPool.Recyle(this.currentAction);
        }
    }
}
