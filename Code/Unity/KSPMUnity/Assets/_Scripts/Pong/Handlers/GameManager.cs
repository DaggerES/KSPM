using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum GameStatus : byte
    {
        None,
        NetworkSettingUp,
        Waiting,
        Starting,
        Playing,
        Finished
    };

    public bool fake;

    public PlayerManager PlayerManagerReference;

    public MovementManager movementManager;

    public GameStatus currentStatus;

    public int RequiredUsers;

    public HostControl[] UserControls;

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
            case GameStatus.Starting:
                this.movementManager.RandomForce();
                this.currentStatus = GameStatus.Playing;
                break;
            case GameStatus.Playing:
                break;
        }
	}

    IEnumerator WaitABit(float time)
    {
        this.currentStatus = GameStatus.Waiting;
        yield return new WaitForSeconds(time);
        this.currentStatus = GameStatus.Starting;
    }

    IEnumerator WaitSome(float time)
    {
        yield return new WaitForSeconds(time);
    }

    /// <summary>
    /// Method used by the server so Don't try to use it inside a client body.
    /// </summary>
    /// <param name="mode"></param>
    public void StartGame(UnityGlobals.WorkingMode mode)
    {
        GameObject goGeneric;
        goGeneric = GameObject.FindGameObjectWithTag("LeftUser");
        this.UserControls[0] = goGeneric.AddComponent<HostControl>();
        this.UserControls[0].target = goGeneric;
        goGeneric = GameObject.FindGameObjectWithTag("RightUser");
        this.UserControls[1] = goGeneric.AddComponent<HostControl>();
        this.PlayerManagerReference.Players[0].GetComponent<MPGamePlayer>().InputControl = this.UserControls[0];
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        /*
        if (this.movementManager != null)
        {
            this.movementManager.WorkingMode = mode;
            StartCoroutine(WaitABit(5f));
        }
        */
    }

    public GameError.ErrorType StartGameAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        GameObject goGeneric;
        goGeneric = GameObject.FindGameObjectWithTag("LeftUser");
        this.UserControls[0] = goGeneric.AddComponent<UserHostControl>();
        this.UserControls[0].target = goGeneric;
        UserHostControl.SetLeftControls((UserHostControl)this.UserControls[0]);
        goGeneric = GameObject.FindGameObjectWithTag("RightUser");
        this.UserControls[1] = goGeneric.AddComponent<UserHostControl>();
        this.PlayerManagerReference.Players[0].GetComponent<GamePlayer>().InputControl = this.UserControls[0];
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        /*
        if (this.movementManager != null)
        {
            this.currentStatus = GameStatus.Waiting;
            return GameError.ErrorType.Ok;
        }
        */
        return GameError.ErrorType.Ok;
    }
}
