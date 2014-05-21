using UnityEngine;
using System.Collections;

public class SceneManager : MonoBehaviour
{
    protected bool working;
    protected AsyncOperation asyncMethod;
    protected float loadingProgress;

    /// <summary>
    /// Delegate definition to load a level asynchronously.
    /// </summary>
    /// <param name="sceneToLoad"></param>
    protected delegate void LoadLevelAsync(SceneManager.Scenes sceneToLoad);

    /// <summary>
    /// Property used to load a level.
    /// </summary>
    protected LoadLevelAsync asynchronousLoader;

    public enum Scenes : byte
    {
        None = 0,
        ClientTest,
        Game,
    };

    public delegate void LoadingCompleteEventHandler(object sender, GameEvenArgs e);

    public event LoadingCompleteEventHandler LoadingComplete;

    void Awake()
    {
        this.working = false;
        
    }

	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this);
        this.asyncMethod = null;
	}

    protected void LoadingCompleteAsync(System.IAsyncResult result)
    {
        this.OnLoadingComplete(result.AsyncState, null);
    }

    public void StartLoadingAsync(Scenes sceneToLoad)
    {
        Application.LoadLevel(sceneToLoad.ToString());
    }

    public IEnumerator LoadLevelAction(object caller, System.Collections.Generic.Stack<object> parameters)
    {
        if (this.working)
        {
            yield return null;
        }
        string levelToLoad = (string)parameters.Pop();
        this.working = true;
        this.asyncMethod = Application.LoadLevelAsync(levelToLoad);
        while (!this.asyncMethod.isDone)
        {
            this.loadingProgress = this.asyncMethod.progress;
            yield return null;
        }
        this.loadingProgress = this.asyncMethod.progress;
        this.OnLoadingComplete(caller, new GameEvenArgs(GameEvenArgs.EventType.GameSceneLoaded, levelToLoad));
    }

    public float LoadingProgress
    {
        get
        {
            return this.loadingProgress;
        }
    }

    protected void OnLoadingComplete(object sender, GameEvenArgs e)
    {
        if (this.LoadingComplete != null)
        {
            this.LoadingComplete(sender, e);
        }
    }
}
