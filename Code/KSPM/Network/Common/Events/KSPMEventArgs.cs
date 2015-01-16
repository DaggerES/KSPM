/*
 * Author: Scr_Ra
 * Date: Jun 5th, 2014
 */

using KSPM.Network.Common;

namespace KSPM.Network.Common.Events
{
    /// <summary>
    /// Delegate definition when a user is connected to the system.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void UserConnectedEventHandler( object sender, KSPMEventArgs e );

    /// <summary>
    /// Delegate definition when a user is disconnected from the system.
    /// KSPMEventArgs has the information.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void UserDisconnectedEventHandler( object sender, KSPMEventArgs e );

    /// <summary>
    /// Delegate definition when a Message arrives through the UDP channel.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public delegate void UDPMessageArrived( object sender, Messages.Message message );

    /// <summary>
    /// Delegate definition when a Message arrives through the TCP channel.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public delegate void TCPMessageArrived( object sender, Messages.Message message );

    /// <summary>
    /// Delegate definition when a information request is completed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void RequestInformationCompleted( object sender, KSPMEventArgs e);
    
    /// <summary>
    /// Defines a wrapper for the details for those delegates which use this class.
    /// </summary>
    public class KSPMEventArgs : System.EventArgs
    {

        /// <summary>
        /// Definition of the type of the event.
        /// </summary>
        public enum EventType : byte
        { 
            /// <summary>
            /// Default value, is harmless.
            /// </summary>
            None = 0,

            /// <summary>
            /// Connection issue.
            /// </summary>
            Connect,

            /// <summary>
            /// Disconnection issue.
            /// </summary>
            Disconnect,

            /// <summary>
            /// An error has caused the event.
            /// </summary>
            Error,

            /// <summary>
            /// A runtime error causes the event, such a event could be originated by a socket issue.
            /// </summary>
            RuntimeError,

            /// <summary>
            /// An information request has been performed.
            /// </summary>
            InformationRequest,
        };

        /// <summary>
        /// Definition for the available causes that could trigger an event.
        /// </summary>
        public enum EventCause : byte
        { 
            /// <summary>
            /// Default value, is harmless.
            /// </summary>
            None = 0,

            /// <summary>
            /// Everything went ok.
            /// </summary>
            Ok,

            /// <summary>
            /// The method was cancelled.
            /// </summary>
            Cancelled,

            /// <summary>
            /// Connection has taken soo long.
            /// </summary>
            ConnectionTimeOut,

            /// <summary>
            /// A connection could not be stablished with the remote host.
            /// </summary>
            TCPHolePunchingCannotBeDone,

            /// <summary>
            /// The server is full.
            /// </summary>
            ServerFull,

            /// <summary>
            /// The remote server closed the socket.
            /// </summary>
            ServerDisconnected,

            /// <summary>
            /// The remote host closed the socket.
            /// </summary>
            ClientDisconnected,

            /// <summary>
            /// A handled exception ocurred so the system can not go further.
            /// </summary>
            ErrorByException,

            /// <summary>
            /// Nice disconnection between the host and the server.
            /// </summary>
            NiceDisconnect,
        };

        /// <summary>
        /// Kind of the event.
        /// </summary>
        public EventType Event;

        /// <summary>
        /// Cause of the event.
        /// </summary>
        public EventCause CauseOfTheEvent;

        /// <summary>
        /// Reference to a token defined by the user. By default null.
        /// </summary>
        public object UserToken;

        /// <summary>
        /// Creates a new wrapper with the given information.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cause"></param>
        public KSPMEventArgs(EventType type, EventCause cause)
        {
            this.Event = type;
            this.CauseOfTheEvent = cause;
            this.UserToken = null;
        }

        /// <summary>
        /// Parses this reference to a string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0}:{1}]", this.Event.ToString(), this.CauseOfTheEvent.ToString());
        }

        /// <summary>
        /// Creates a new reference with the values set to default.
        /// </summary>
        new public static KSPMEventArgs Empty
        {
            get
            {
                return new KSPMEventArgs(EventType.None, EventCause.None);
            }
        }
    }
}
