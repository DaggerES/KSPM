using UnityEngine;
using System.Collections.Generic;
using KSPM.Network.Server;
using KSPM.Network.Client;

public class PlayerManager : MonoBehaviour
{
    public List<GameObject> Players;
    protected static int PlayerCounter = 0;

    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public GameObject CreateEmptyPlayer()
    {
        GameObject go = null;
        int id = System.Threading.Interlocked.Increment(ref PlayerManager.PlayerCounter);
        go = new GameObject();
        go.name = string.Format("Player_{0}", id);
        this.Players.Add(go);
        return go;
    }

    public GameError.ErrorType CreateEmptyPlayerAction(object caller, Stack<object> parameters)
    {
        ServerSideClient ssClientConnected = (ServerSideClient)caller;
        GameObject go = null;
        int id = System.Threading.Interlocked.Increment(ref PlayerManager.PlayerCounter);
        MPGamePlayer mpPlayer;
        go = new GameObject(string.Format("Player_{0}", id));
        DontDestroyOnLoad(go);///Setting the flag to avoid being destroyed by the Unity GC.
        ssClientConnected.gameUser.UserDefinedHolder = go;
        mpPlayer = go.AddComponent<MPGamePlayer>();
        this.Players.Add(go);
        parameters.Push(go);
        return GameError.ErrorType.Ok;
    }

    public GameError.ErrorType CreateEmptyLocalPlayerAction(object caller, Stack<object> parameters)
    {
        GameClient gameClientConnected = (GameClient)caller;
        GameObject go = null;
        int id = System.Threading.Interlocked.Increment(ref PlayerManager.PlayerCounter);
        GamePlayer localPlayer;
        go = new GameObject(string.Format("Player_{0}", id));
        DontDestroyOnLoad(go);///Setting the flag to avoid being destroyed by the Unity GC.
        gameClientConnected.ClientOwner.UserDefinedHolder = go;
        localPlayer = go.AddComponent<GamePlayer>();
        this.Players.Add(go);
        parameters.Push(go);
        return GameError.ErrorType.Ok;
    }

    public GameError.ErrorType StopPlayer(object caller, Stack<object> parameters)
    {
        GameObject go = (GameObject)caller;
        go.GetComponent<GamePlayer>().Release();
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }
}
