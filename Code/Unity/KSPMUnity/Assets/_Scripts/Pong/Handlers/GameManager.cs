using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum GameStatus : byte
    {
        None,

        /// <summary>
        /// To tell that they are negotiating network stuff and the like.
        /// </summary>
        NetworkSettingUp,

        /// <summary>
        /// Idle status.
        /// </summary>
        Waiting,

        /// <summary>
        /// Thinking to use it to set tthe game.
        /// </summary>
        Starting,

        /// <summary>
        /// Tells that everything is ready to start.
        /// </summary>
        ReadyToStart,
        Playing,
        Finished
    };

    public bool fake;

    public PlayerManager PlayerManagerReference;

    public MovementManager movementManager;

    public GameStatus currentStatus;

    public int RequiredUsers;

    public System.Collections.Generic.List<IPersistentAttribute<Vector3>> WorldPositions;

    void Awake()
    {
        DontDestroyOnLoad(this);
        this.WorldPositions = new System.Collections.Generic.List<IPersistentAttribute<Vector3>>();
    }

	// Use this for initialization
	void Start ()
    {
        if (this.fake)
        {
            this.currentStatus = GameStatus.Starting;
        }
        else
        {
            this.currentStatus = GameStatus.None;
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
        switch (this.currentStatus)
        {
            case GameStatus.Waiting:
                break;
            case GameStatus.ReadyToStart:
                break;
            case GameStatus.Starting:
                break;
            case GameStatus.Playing:
                break;
        }
	}

    /// <summary>
    /// Method used by the server so Don't try to use it inside a client body.
    /// </summary>
    /// <param name="mode"></param>
    public void StartGame(UnityGlobals.WorkingMode mode)
    {
        GameObject goGeneric;
        HostControl hostControl;
        GamePlayer gamePlayer;
        for (int i = 0; i < this.PlayerManagerReference.Players.Count; i++)
        {
            gamePlayer = this.PlayerManagerReference.Players[i].GetComponent<GamePlayer>();
            switch (gamePlayer.GamingRol)
            {
                case PlayerManager.GameRol.Host:
                    goGeneric = GameObject.FindGameObjectWithTag("LeftUser");
                    hostControl = goGeneric.AddComponent<HostControl>();
                    hostControl.target = goGeneric;
                    hostControl.SetDebug();
                    gamePlayer.InputControl = hostControl;
                    this.WorldPositions.Add(hostControl);
                    break;
                case PlayerManager.GameRol.Remote:
                    goGeneric = GameObject.FindGameObjectWithTag("RightUser");
                    hostControl = goGeneric.AddComponent<HostControl>();
                    hostControl.target = goGeneric;
                    hostControl.SetDebug();
                    gamePlayer.InputControl = hostControl;
                    this.WorldPositions.Add(hostControl);
                    break;
                case PlayerManager.GameRol.Spectator:
                    goGeneric = new GameObject();
                    hostControl = goGeneric.AddComponent<HostControl>();
                    hostControl.target = goGeneric;
                    hostControl.SetDebug();
                    gamePlayer.InputControl = hostControl;
                    break;
            }
        }
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        
        if (this.movementManager != null)
        {
            this.WorldPositions.Add(this.movementManager);
            this.movementManager.WorkingMode = mode;
            this.currentStatus = GameStatus.ReadyToStart;
        }
    }

    /// <summary>
    /// Method used by the client to initialize the scene game.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public GameError.ErrorType StartGameAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        GameObject goGeneric;
        GamePlayer playerObject;
        UserHostControl inputControl;
        for (int i = 0; i < this.PlayerManagerReference.Players.Count; i++)
        {
            playerObject = this.PlayerManagerReference.Players[i].GetComponent<GamePlayer>();
            switch (playerObject.GamingRol)
            {
                case PlayerManager.GameRol.Host:
                    if (playerObject.IsLocal)
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("LeftUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        inputControl.enabled = false;
                        UserHostControl.SetLeftControls(ref inputControl);
                        inputControl.target = goGeneric;
                        inputControl.Owner = playerObject;
                        inputControl.SetDebug();
                        playerObject.InputControl = inputControl;
                    }
                    else
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("LeftUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        inputControl.enabled = false;
                        UserHostControl.SetRemoteControls(ref inputControl);
                        inputControl.target = goGeneric;
                        inputControl.Owner = playerObject;
                        inputControl.SetDebug();
                        playerObject.InputControl = inputControl;
                    }
                    this.WorldPositions.Add(inputControl);
                    break;
                case PlayerManager.GameRol.Remote:
                    if (playerObject.IsLocal)
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("RightUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        UserHostControl.SetRightControls(ref inputControl);
                        inputControl.enabled = false;
                        inputControl.target = goGeneric;
                        inputControl.Owner = playerObject;
                        inputControl.SetDebug();
                        playerObject.InputControl = inputControl;
                    }
                    else
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("RightUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        inputControl.enabled = false;
                        UserHostControl.SetRemoteControls(ref inputControl);
                        inputControl.target = goGeneric;
                        inputControl.Owner = playerObject;
                        inputControl.SetDebug();
                        playerObject.InputControl = inputControl;
                    }
                    this.WorldPositions.Add(inputControl);
                    break;
                case PlayerManager.GameRol.Spectator:
                    goGeneric = this.PlayerManagerReference.Players[i];
                    inputControl = goGeneric.AddComponent<UserHostControl>();
                    inputControl.target = goGeneric;
                    inputControl.enabled = false;
                    inputControl.Owner = playerObject;
                    inputControl.SetDebug();
                    UserHostControl.SetRemoteControls(ref inputControl);
                    playerObject.InputControl = inputControl;
                    break;
            }
            playerObject.Ready = true;
        }

        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();

        if (this.movementManager != null)
        {
            this.WorldPositions.Add(this.movementManager);
            this.currentStatus = GameStatus.Waiting;
            return GameError.ErrorType.Ok;
        }
        return GameError.ErrorType.Ok;
    }

    /// <summary>
    /// Method called by the clients to set each of the
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public GameError.ErrorType SetPlayersEnableValueAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        bool enableValue = (bool)parameters.Pop();
        for (int i = 0; i < this.PlayerManagerReference.Players.Count; i++)
        {
            this.PlayerManagerReference.Players[i].GetComponent<GamePlayer>().InputControl.enabled = enableValue;
        }
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }

    /// <summary>
    /// Method used by each client to update everything with the data incoming from the server.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public GameError.ErrorType WorldUpdateAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        float x, y, z;
        int itemsCount;
        Vector3 positionParameter = Vector3.zero;
        itemsCount = (int)parameters.Pop();
        for (int i = 0; i < itemsCount; i++)
        {
            x = (float)parameters.Pop();
            y = (float)parameters.Pop();
            z = (float)parameters.Pop();
            positionParameter.Set(x, y, z);
            this.WorldPositions[(this.WorldPositions.Count - 1) - i].UpdatePersistentValue(positionParameter);
        }
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }

    public GameError.ErrorType StopPlayer(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        GameObject go = (GameObject)caller;
        GamePlayer player = go.GetComponent<GamePlayer>();
        this.WorldPositions.Remove(player.InputControl);
        this.PlayerManagerReference.RemovePlayer(ref player);

        parameters.Push(player.GameId);

        player.Release();
        GameObject.Destroy(player.gameObject);
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }
}
