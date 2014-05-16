using UnityEngine;
using System.Collections;

public class SceneManager : MonoBehaviour
{
    protected bool working;
    protected AsyncOperation asyncMethod;
    protected float loadingProgress;

    public enum Scenes : byte
    {
        None = 0,
        ClientTest,
        Game,
    };

    public delegate void LoadingCompleteEventHandler(object sender, System.EventArgs e);

    public event LoadingCompleteEventHandler LoadingComplete;

	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this);
        this.working = false;
        this.asyncMethod = null;
	}

    public IEnumerator LoadLevel(string levelName)
    {
        if (this.working)
        {
            yield return null;
        }
        this.working = true;
        this.asyncMethod = Application.LoadLevelAsync(levelName);
        while( !this.asyncMethod.isDone )
        {
            this.loadingProgress = this.asyncMethod.progress;
            yield return null;
        }
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

    public void OnLoadingComplete(object sender, System.EventArgs e)
    {
        if (this.LoadingComplete != null)
        {
            this.LoadingComplete(sender, e);
        }
    }
}
