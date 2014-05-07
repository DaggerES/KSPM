using KSPM.Network.Common;

namespace KSPM.Network.Common.Events
{
    public delegate void UserConnectedEventHandler( object sender, KSPMEventArgs e );
    public delegate void UserDisconnectedEventHandler( object sender, KSPMEventArgs e );

    public delegate void UDPMessageArrived( object sender, Messages.Message message );
    
    public class KSPMEventArgs : System.EventArgs
    {
        public enum EventType : byte
        { 
            None = 0,
            Connect,
            Disconnect,
            Error,
            RuntimeError,
        };

        public enum EventCause : byte
        { 
            None = 0,
            ConnectionTimeOut,
            TCPHolePunchingCannotBeDone,
            ServerFull,
            ServerDisconnected,

            ErrorByException,

            NiceDisconnect,
        };

        public EventType Event;
        public EventCause CauseOfTheEvent;

        public KSPMEventArgs(EventType type, EventCause cause)
        {
            this.Event = type;
            this.CauseOfTheEvent = cause;
        }

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", this.Event.ToString(), this.CauseOfTheEvent.ToString());
        }

        new public static KSPMEventArgs Empty
        {
            get
            {
                return new KSPMEventArgs(EventType.None, EventCause.None);
            }
        }
    }
}
