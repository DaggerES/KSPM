public class TickController
{
	public System.Threading.Timer tickTimer;
    public System.Threading.TimerCallback tickCallback;
    protected long tickInterval;
    protected bool enabled;

    public TickController(long tickInterval, object referenceToTheKSPMServerSideClient, System.Threading.TimerCallback callback)
    {
        this.tickInterval = tickInterval;
        this.tickCallback = callback;
        this.tickTimer = new System.Threading.Timer(this.tickCallback, referenceToTheKSPMServerSideClient, this.tickInterval, this.tickInterval);
        this.enabled = true;
    }

    public void Enable(long tickInterval)
    {
        this.tickInterval = tickInterval;
        this.tickTimer.Change(this.tickInterval, this.tickInterval);
        this.enabled = true;
    }

    public void Disable()
    {
        this.tickTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        this.enabled = false;
    }

    public void Release()
    {
        this.tickTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        this.tickTimer.Dispose();
        this.tickTimer = null;
        this.tickCallback = null;
        this.enabled = false;
    }

    public bool Enabled
    {
        get
        {
            return this.enabled;
        }
    }
}
