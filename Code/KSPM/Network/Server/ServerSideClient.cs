using System.Net;
using System.Net.Sockets;
using System.Threading;
using KSPM.Network.Common;
using KSPM.Network.Common.Packet;
using KSPM.Globals;


namespace KSPM.Network.Server
{
    /// <summary>
    /// Represents a client handled by the server.
    /// </summary>
    public class ServerSideClient : NetworkEntity
    {
        /// <summary>
        /// ServerSide status.
        /// </summary>
        public enum ClientStatus : byte { Handshaking = 0, Handshaked, Authenticating, UDPConnecting, Connected, AwaitingACK};

        /// <summary>
        /// Thread to run the main body of the thread.
        /// </summary>
        protected Thread mainThread;

        /// <summary>
        /// Constrols the mainThread lifecycle.
        /// </summary>
        protected bool aliveFlag;

        /// <summary>
        /// Tells if the client is ready to run.
        /// </summary>
        protected bool ableToRun;

        protected ClientStatus currentStatus;

        protected ServerSideClient() : base()
        {
            this.currentStatus = ClientStatus.Handshaking;
            this.mainThread = new Thread(new ThreadStart(this.HandleMainBodyMethod));
            this.ableToRun = true;
        }

        /// <summary>
        /// Creates a ServerSideCliente object from a NetworkEntity reference and then disclose the network entity.
        /// </summary>
        /// <param name="baseNetworkEntity">Reference (ref) to the NetwrokEntity used as a base to create the new ServerSideClient object.</param>
        /// <param name="ssClient">New server side clint out reference.</param>
        /// <returns></returns>
        public static Error.ErrorType CreateFromNetworkEntity(ref NetworkEntity baseNetworkEntity, out ServerSideClient ssClient )
        {
            ssClient = null;
            if (baseNetworkEntity == null)
            {
                return Error.ErrorType.InvalidNetworkEntity;
            }
            ssClient = new ServerSideClient();
            ssClient.ownerSocket = baseNetworkEntity.ownerSocket;
            ssClient.rawBuffer = baseNetworkEntity.rawBuffer;
            baseNetworkEntity.Dispose();
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Handles the main behaviour of the server side clien.
        /// </summary>
        protected void HandleMainBodyMethod()
        {
            Message tempMessage = null;
            NetworkEntity myNetorkEntityReference = this;

            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return;
            }

            while (this.aliveFlag)
            {
                switch (this.currentStatus)
                {
                    case ClientStatus.Handshaking:
                        Message.HandshakeAccetpMessage(ref myNetorkEntityReference);
                        PacketHandler.EncodeMessage(ref myNetorkEntityReference, out tempMessage);
                        KSPMGlobals.Globals.KSPMServer.outgoingMessagesQueue.EnqueueCommandMessage(ref tempMessage);
                        this.currentStatus = ClientStatus.AwaitingACK;
                        break;
                    case ClientStatus.AwaitingACK:
                        break;
                    case ClientStatus.Handshaked:
                        break;
                }
                Thread.Sleep(3);
            }
        }

        /// <summary>
        /// Starts the client so it is going to be able to live in another thread.
        /// </summary>
        /// <returns></returns>
        public bool StartClient()
        {
            bool result = false;
            if (!this.ableToRun)
            {
                KSPMGlobals.Globals.Log.WriteTo(Error.ErrorType.ServerClientUnableToRun.ToString());
                return false;
            }
            try
            {
                this.mainThread.Start();
                result = true;
            }
            catch( System.Exception ex )
            {
                KSPMGlobals.Globals.Log.WriteTo(ex.Message);
                this.ableToRun = false;
            }
            return result;
        }

        /// <summary>
        /// Stops the current client and make it unable to run again.
        /// </summary>
        public void ShutdownClient()
        {
            ///***********************Killing threads code
            this.aliveFlag = false;
            this.mainThread.Abort();
            this.mainThread.Join();
            KSPMGlobals.Globals.Log.WriteTo("Killed mainThread ...");

            ///***********************Sockets code
            if (this.ownerSocket.Connected)
            {
                this.ownerSocket.Shutdown(SocketShutdown.Both);
                this.ownerSocket.Close();
            }
            this.ownerSocket = null;
            this.rawBuffer = null;

            this.ableToRun = false;

            KSPMGlobals.Globals.Log.WriteTo("ServerSide Client killed!!!");
        }

        /// <summary>
        /// Changes the Awating status from the client, this does not require be thread safe.
        /// </summary>
        /// <param name="newStatus"></param>
        public void RemoveAwaitingState(ServerSideClient.ClientStatus newStatus)
        {
            this.currentStatus = newStatus;
        }

        public void AsyncSenderCallback(System.IAsyncResult result)
        {
            int sentBytes;
            Socket callingSocket = null;
            try
            {
                callingSocket = (Socket)result.AsyncState;
                sentBytes = callingSocket.EndSend(result);
            }
            catch (System.Exception)
            {
            }
        }
    }
}
