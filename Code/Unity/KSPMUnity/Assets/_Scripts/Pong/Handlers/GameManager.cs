using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum GameStatus : byte
    {
        None,
        NetworkSettingUp,
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
            case GameStatus.Starting:
                this.movementManager.RandomForce();
                this.currentStatus = GameStatus.Playing;
                break;
            case GameStatus.Playing:
                break;
        }
	}

    public void StartGame()
    {
        this.movementManager = GameObject.FindGameObjectWithTag("GameLogic").GetComponent<MovementManager>();
        if (this.movementManager != null)
        {
            this.currentStatus = GameStatus.Starting;
        }
    }
}
