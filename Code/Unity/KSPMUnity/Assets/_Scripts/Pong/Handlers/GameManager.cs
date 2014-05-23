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
    }

    public bool fake;

    public MovementManager movementManager;

    public GameStatus currentStatus;

    public int RequiredUsers;

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

    public void StartGame(UnityGlobals.WorkingMode mode)
    {
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        if (this.movementManager != null)
        {
            this.movementManager.WorkingMode = mode;
            StartCoroutine(WaitABit(5f));
        }
    }

    public GameError.ErrorType StartGameAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        StartCoroutine(WaitSome(2));
        Debug.Log("Buscando");
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        if (this.movementManager != null)
        {
            this.currentStatus = GameStatus.Waiting;
            return GameError.ErrorType.Ok;
        }
        return GameError.ErrorType.Ok;
    }
}
