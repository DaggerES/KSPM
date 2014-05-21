using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class KSPMAction
{
    public delegate IEnumerator IEnumerateAction<T,U>(T caller, System.Collections.Generic.Stack<U> parameters);
    public delegate GameError.ErrorType Action<T,U>(T caller, System.Collections.Generic.Stack<U> parameters);

    public System.Collections.Generic.Stack<object> ParametersStack;

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
        public IEnumerateAction<object,object> EnumeratedAction;

        [FieldOffset(0)]
        public Action<object,object> BasicAction;
    };
    
    public ActionWrapper ActionMethod;

    public ActionType ActionKind;

    public KSPMAction()
    {
        this.ActionKind = ActionType.Null;
        this.ParametersStack = new System.Collections.Generic.Stack<object>();
    }

    public virtual KSPMAction Empty()
    {
        return new KSPMAction();
    }

    public virtual void Release()
    {
        this.ActionKind = ActionType.Null;
        this.ParametersStack.Clear();
        this.ParametersStack = null;
    }

    public virtual void Dispose()
    {
        this.ActionKind = ActionType.Null;
        this.ParametersStack.Clear();
    }
}
