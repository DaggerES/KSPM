using UnityEngine;
using System.Collections.Generic;
using KSPM.Network.Server;
using KSPM.Network.Client;

public class PlayerManager : MonoBehaviour
{
    public List<GameObject> Players;
    protected Dictionary<int, GamePlayer> PlayersInternalStructure;
    protected static int PlayerCounter = 0;
    protected GameRol GamingRolesFlag = 0;

    public enum GameRol : byte
    {
        Undefined = 0,
        Spectator,
        Host,
        Remote,
        Left,
        Right,
    };

    void Awake()
    {
        DontDestroyOnLoad(this);
        this.PlayersInternalStructure = new Dictionary<int, GamePlayer>();
    }

    /// <summary>
    /// Method used by the server to create an empty player.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
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
        mpPlayer.GameId = id;
        mpPlayer.Parent = ssClientConnected;
        this.SetGamingRolToPlayer(mpPlayer);

        this.Players.Add(go);
        parameters.Push(go);
        return GameError.ErrorType.Ok;
    }

    /// <summary>
    /// Method used by the client to create empty players.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public GameError.ErrorType CreateEmptyLocalPlayerAction(object caller, Stack<object> parameters)
    {
        GameClient gameClientConnected = (GameClient)caller;
        GameObject go = null;
        GameRol gamingRol = (GameRol)parameters.Pop();
        int localPlayerId = (int)parameters.Pop();
        int remotePlayerId = (int)parameters.Pop();
        GameRol remotePlayerGamingRol = (GameRol)parameters.Pop();
        GamePlayer localPlayer;

        go = new GameObject(string.Format("Player_{0}", remotePlayerId));
        DontDestroyOnLoad(go);///Setting the flag to avoid being destroyed by the Unity GC.

        localPlayer = go.AddComponent<GamePlayer>();
        localPlayer.GamingRol = remotePlayerGamingRol;
        localPlayer.GameId = remotePlayerId;

        if (localPlayerId == remotePlayerId)
        {
            gameClientConnected.ClientOwner.UserDefinedHolder = go;
            localPlayer.Parent = gameClientConnected;
            localPlayer.SetLocal(true);
        }
        this.Players.Add(go);
        this.PlayersInternalStructure.Add(localPlayer.GameId, localPlayer);
        parameters.Push(go);
        return GameError.ErrorType.Ok;
    }

    /// <summary>
    /// Method called by the clients to verify its own GamePlayer List, and to reorder the GamePlayer to match with the GameServer itself.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public GameError.ErrorType CheckGamePlayersSummaryAction(object caller, Stack<object> parameters)
    {
        int playerId;
        int playersCount;
        GameRol gamingRol;
        GameObject go = null;
        GamePlayer localPlayer;
        int[] playersId_ServersOrder;

        playersCount = (int)parameters.Pop();
        playersId_ServersOrder = new int[playersCount];
        for (int i = 0; i < playersCount; i++)
        {
            playerId = (int)parameters.Pop();
            gamingRol = (GameRol)parameters.Pop();
            playersId_ServersOrder[i] = playerId;
            if (!this.PlayersInternalStructure.ContainsKey(playerId))
            {
                go = new GameObject(string.Format("Player_{0}", playerId));
                DontDestroyOnLoad(go);///Setting the flag to avoid being destroyed by the Unity GC.

                localPlayer = go.AddComponent<GamePlayer>();
                localPlayer.GamingRol = gamingRol;
                localPlayer.GameId = playerId;

                this.Players.Add(go);
                this.PlayersInternalStructure.Add(localPlayer.GameId, localPlayer);
            }
        }
        System.Array.Reverse(playersId_ServersOrder);
        ///Reordering players.

        this.Players.Clear();
        for (int i = 0; i < playersId_ServersOrder.Length; i++)
        {
            if (this.PlayersInternalStructure.TryGetValue(playersId_ServersOrder[i], out localPlayer))
            {
                this.Players.Add(localPlayer.gameObject);
            }
        }


        ///Pushing the GamePlayer object into the stack.
        ///
        /*
        localPlayer = ((GameObject)((GameClient)caller).ClientOwner.UserDefinedHolder).GetComponent<GamePlayer>();
        parameters.Push(localPlayer);*/
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }

    public GameError.ErrorType SetPlayerReady(object caller, Stack<object> parameters)
    {
        ServerSideClient remoteClient = (ServerSideClient)caller;
        MPGamePlayer remotePlayerObject = ((GameObject)remoteClient.gameUser.UserDefinedHolder).GetComponent<MPGamePlayer>();
        bool readyFlag = (bool)parameters.Pop();
        remotePlayerObject.Ready = readyFlag;
        readyFlag = true;

        ///Reading the ready flag of every gameplayer.
        for (int i = 0; i < this.Players.Count; i++)
        {
            if (!((GameObject)this.Players[i]).GetComponent<MPGamePlayer>().Ready)
            {
                readyFlag = false;
                break;
            }
        }
        parameters.Push(readyFlag);
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }

    public GameError.ErrorType StopPlayer(object caller, Stack<object> parameters)
    {
        GameObject go = (GameObject)caller;
        GamePlayer player = go.GetComponent<GamePlayer>();
        player.Release();
        parameters.Push(player);
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }

    public void SetGamingRolToPlayer(GamePlayer target)
    {
        switch (this.GamingRolesFlag)
        {
            case GameRol.Undefined:
                this.GamingRolesFlag = GameRol.Host;
                break;
            case GameRol.Host:
                this.GamingRolesFlag = GameRol.Remote;
                break;
            case GameRol.Remote:
                this.GamingRolesFlag = GameRol.Spectator;
                break;
            case GameRol.Spectator:
            default:
                this.GamingRolesFlag = GameRol.Spectator;
                break;
        }
        target.GamingRol = this.GamingRolesFlag;
    }

    public void RemovePlayer(ref GamePlayer player)
    {
        this.PlayersInternalStructure.Remove(player.GameId);
        this.Players.Remove(player.gameObject);
    }
}
