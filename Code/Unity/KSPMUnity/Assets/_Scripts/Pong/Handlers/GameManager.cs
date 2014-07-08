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

    public System.Collections.Generic.List<HostControl> UserControls;

    void Awake()
    {
        DontDestroyOnLoad(this);
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
                    this.UserControls.Add(hostControl);
                    gamePlayer.InputControl = hostControl;
                    break;
                case PlayerManager.GameRol.Remote:
                    goGeneric = GameObject.FindGameObjectWithTag("RightUser");
                    hostControl = goGeneric.AddComponent<HostControl>();
                    hostControl.target = goGeneric;
                    gamePlayer.InputControl = hostControl;
                    this.UserControls.Add(hostControl);
                    break;
                case PlayerManager.GameRol.Spectator:
                    goGeneric = new GameObject();
                    hostControl = goGeneric.AddComponent<HostControl>();
                    hostControl.target = goGeneric;
                    gamePlayer.InputControl = hostControl;
                    this.UserControls.Add(hostControl);
                    break;
            }
        }
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        
        if (this.movementManager != null)
        {
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
                        playerObject.InputControl = inputControl;
                        this.UserControls.Add(inputControl);
                    }
                    else
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("LeftUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        inputControl.enabled = false;
                        UserHostControl.SetRemoteControls(ref inputControl);
                        inputControl.target = goGeneric;
                        playerObject.InputControl = inputControl;
                        this.UserControls.Add(inputControl);
                    }
                    break;
                case PlayerManager.GameRol.Remote:
                    if (playerObject.IsLocal)
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("RightUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        UserHostControl.SetRightControls(ref inputControl);
                        inputControl.enabled = false;
                        inputControl.target = goGeneric;
                        playerObject.InputControl = inputControl;
                        this.UserControls.Add(inputControl);
                    }
                    else
                    {
                        goGeneric = GameObject.FindGameObjectWithTag("RightUser");
                        inputControl = goGeneric.AddComponent<UserHostControl>();
                        inputControl.enabled = false;
                        UserHostControl.SetRemoteControls(ref inputControl);
                        inputControl.target = goGeneric;
                        playerObject.InputControl = inputControl;
                        this.UserControls.Add(inputControl);
                    }
                    break;
                case PlayerManager.GameRol.Spectator:
                    goGeneric = this.PlayerManagerReference.Players[i];
                    inputControl = goGeneric.AddComponent<UserHostControl>();
                    inputControl.target = goGeneric;
                    inputControl.enabled = false;
                    UserHostControl.SetRemoteControls(ref inputControl);
                    playerObject.InputControl = inputControl;
                    this.UserControls.Add(inputControl);
                    break;
            }
            playerObject.Ready = true;
        }

        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();

        if (this.movementManager != null)
        {
            this.currentStatus = GameStatus.Waiting;
            return GameError.ErrorType.Ok;
        }
        return GameError.ErrorType.Ok;
    }

    public GameError.ErrorType SetPlayersEnableValueAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        bool enableValue = (bool)parameters.Pop();
        for (int i = 0; i < this.PlayerManagerReference.Players.Count; i++)
        {
            this.PlayerManagerReference.Players[i].GetComponent<GamePlayer>().InputControl.enabled = enableValue;
        }
        return KSPM.Network.Common.Error.ErrorType.Ok;
    }

}
