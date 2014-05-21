public class GameEvenArgs : System.EventArgs
{
    public enum EventType : byte
    {
        None = 0,

        GameSceneLoaded,
    }

    public EventType Event;
    public object EventParameter;

    public GameEvenArgs(EventType kindOfTheEvent, object parameter)
    {
        this.Event = kindOfTheEvent;
        this.EventParameter = parameter;
    }
}
