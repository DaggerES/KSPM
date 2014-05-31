using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class KSPMAction<T, U>
{
    public delegate IEnumerator IEnumerateAction<T,U>(T caller, System.Collections.Generic.Stack<U> parameters);
    public delegate GameError.ErrorType Action<T,U>(T caller, System.Collections.Generic.Stack<U> parameters);
    public delegate void ActionCompleted(object caller, System.Collections.Generic.Stack<U> parameters);

    public System.Collections.Generic.Stack<U> ParametersStack;

    public enum ActionType : byte
    {
        Null = 0,
        EnumeratedMethod,
        NormalMethod,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ActionWrapper
    {
        [FieldOffset(0)]
        public IEnumerateAction<T, U> EnumeratedAction;

        [FieldOffset(0)]
        public Action<T,U> BasicAction;
    };
    
    public ActionWrapper ActionMethod;

    public ActionType ActionKind;

    public event ActionCompleted Completed;

    public KSPMAction()
    {
        this.ActionKind = ActionType.Null;
        this.ParametersStack = new System.Collections.Generic.Stack<U>();
        this.Completed = null;
    }

    internal void OnActionCompleted(object caller, System.Collections.Generic.Stack<U> stackParameter)
    {
        if (this.Completed != null)
        {
            this.Completed(caller, stackParameter);
        }
    }

    public virtual KSPMAction<T,U> Empty()
    {
        return new KSPMAction<T, U>();
    }

    public virtual void Release()
    {
        this.ActionKind = ActionType.Null;
        this.ParametersStack.Clear();
        this.ParametersStack = null;
        this.Completed = null;
    }

    public virtual void Dispose()
    {
        this.ActionKind = ActionType.Null;
        this.ParametersStack.Clear();
        this.Completed = null;
    }
}
