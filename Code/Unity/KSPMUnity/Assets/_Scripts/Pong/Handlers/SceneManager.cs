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

    public delegate void LoadingCompleteEventHandler(object sender, System.EventArgs e);

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

    public void LoadLevel(Scenes sceneToLoad)
    {
        /*
        if (this.working)
            return;
        this.working = true;
        Debug.Log("CALLED");
        this.asynchronousLoader = new LoadLevelAsync(this.StartLoadingAsync);
        this.asynchronousLoader.BeginInvoke(sceneToLoad, this.LoadingCompleteAsync, sceneToLoad);
        */
    }

    
    public IEnumerator LoadLevel(object caller, object levelName)
    {
        if (this.working)
        {
            yield return null;
        }
        this.working = true;
        this.asyncMethod = Application.LoadLevelAsync((string)levelName);
        Debug.Log("Complete_1");
        while( !this.asyncMethod.isDone )
        {
            this.loadingProgress = this.asyncMethod.progress;
            yield return null;
        }
        Debug.Log("Complete_2");
        this.loadingProgress = this.asyncMethod.progress;
        this.OnLoadingComplete(levelName, System.EventArgs.Empty);
    }

    public float LoadingProgress
    {
        get
        {
            return this.loadingProgress;
        }
    }

    protected void OnLoadingComplete(object sender, System.EventArgs e)
    {
        Debug.Log("Complete");
        if (this.LoadingComplete != null)
        {
            Debug.Log("Callings");
            this.LoadingComplete(sender, e);
        }
    }
}
