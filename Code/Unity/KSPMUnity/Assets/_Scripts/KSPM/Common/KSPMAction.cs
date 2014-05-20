using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class KSPMAction
{
    public delegate GameError.ErrorType ActionSingleParameter(object caller, object parameter);
    public delegate IEnumerator IEnumerateActionSincleParameter(object caller, object parameter);

    public enum ActionType : byte
    {
        Null = 0,
        LoadScene
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ActionMethod
    {
        [FieldOffset(0)]
        public ActionSingleParameter ActionSingleParameterMethod;
        
        [FieldOffset(0)]
        public IEnumerateActionSincleParameter IEnumerateActionMethod;
    };

    public ActionMethod method;

    public IEnumerateActionSincleParameter IEnumerateActionMethod;
    public ActionType Action;
    public object actionParameter;

    public KSPMAction(ActionType action, object parameter)
    {
        this.Action = action;
        this.actionParameter = parameter;
    }
}
