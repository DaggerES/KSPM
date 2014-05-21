
/// <summary>
/// Provides a preloaded pool of KSPMActions.
/// </summary>
public class KSPMActionsPool
{
    protected System.Collections.Generic.Queue<KSPMAction> pool;

    protected uint poolSize;

    protected KSPMAction sample;

    public KSPMActionsPool(uint poolSize, KSPMAction actionSample)
    {
        this.poolSize = poolSize;
        this.sample = actionSample;
        this.pool = new System.Collections.Generic.Queue<KSPMAction>((int)this.poolSize);
        for (int i = 0; i < this.poolSize; i++)
        {
            this.pool.Enqueue(this.sample.Empty());
        }
    }

    public void Release()
    {
        KSPMAction action = null;
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

    public KSPMAction BorrowAction
    {
        get
        {
            KSPMAction borrowedAction = null;
            lock (this.pool)
            {
                if (this.pool.Count > 0)
                {
                    borrowedAction = this.pool.Dequeue();
                }
            }
            if (borrowedAction == null)
            {
                borrowedAction = this.sample.Empty();
            }
            return borrowedAction;
        }
    }

    public void Recyle(KSPMAction oldItem)
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
