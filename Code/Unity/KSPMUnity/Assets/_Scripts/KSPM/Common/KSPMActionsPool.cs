
/// <summary>
/// Provides a preloaded pool of KSPMActions.
/// </summary>
public class KSPMActionsPool<T, U>
{
    protected System.Collections.Generic.Queue<KSPMAction<T,U>> pool;

    /// <summary>
    /// The initial size of the pool.<b>Keep in mind the this size can chage during the runtime.</b>
    /// </summary>
    protected uint poolSize;

    /// <summary>
    /// Sample of the objects held by the pool.
    /// </summary>
    protected KSPMAction<T,U> sample;

    public KSPMActionsPool(uint poolSize, KSPMAction<T,U> actionSample)
    {
        this.poolSize = poolSize;
        this.sample = actionSample;
        this.pool = new System.Collections.Generic.Queue<KSPMAction<T,U>>((int)this.poolSize);
        for (int i = 0; i < this.poolSize; i++)
        {
            this.pool.Enqueue(this.sample.Empty());
        }
    }

    public void Release()
    {
        KSPMAction<T,U> action = null;
        lock (this.pool)
        {
            while (this.pool.Count > 0)
            {
                action = this.pool.Dequeue();
                action.Release();
                action = null;
            }
        }
        this.pool = null;
        this.poolSize = 0;
        this.sample.Release();
        this.sample = null;
    }

    /// <summary>
    /// Gets a new KSPMAction from the pool.<b>If it is empty, a new one is created and returned.</b>
    /// </summary>
    public KSPMAction<T,U> BorrowAction
    {
        get
        {
            KSPMAction<T,U> borrowedAction = null;
            lock (this.pool)
            {
                if (this.pool.Count > 0)
                {
                    borrowedAction = this.pool.Dequeue();
                }
            }
            if (borrowedAction == null)
            {
                UnityEngine.Debug.LogWarning(string.Format("[{0}]-Empty pool, creating a new one.", this.pool.GetType().ToString()));
                borrowedAction = this.sample.Empty();
            }
            return borrowedAction;
        }
    }

    public void Recyle(KSPMAction<T,U> oldItem)
    {
        if (oldItem == null)
            return;
        oldItem.Dispose();
        lock (this.pool)
        {
            this.pool.Enqueue(oldItem);
        }
    }
}
