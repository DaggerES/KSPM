
public class ServerSideClientController
{
    public System.Threading.Timer tickTimer;
    public System.Threading.TimerCallback tickCallback;
    protected long tickInterval;

    public ServerSideClientController(long tickInterval, object referenceToTheKSPMServerSideClient, System.Threading.TimerCallback callback)
    {
        this.tickInterval = tickInterval;
        this.tickCallback = callback;
        this.tickTimer = new System.Threading.Timer(this.tickCallback, referenceToTheKSPMServerSideClient, this.tickInterval, this.tickInterval);
        //this.tickTimer.Change(this.tickInterval, this.tickInterval);
    }

    public void Release()
    {
        this.tickTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        this.tickTimer.Dispose();
        this.tickTimer = null;
        this.tickCallback = null;
    }
}
