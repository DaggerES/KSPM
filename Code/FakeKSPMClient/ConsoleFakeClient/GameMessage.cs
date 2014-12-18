
using System.Collections;

using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common;
using KSPM.Network.Server;

/// <summary>
/// Messages to be sent throught the TCP channel.<b>It is so important, because it writes into the tcp buffer.</b>
/// </summary>
public class GameMessage : ManagedMessage
{
    /// <summary>
    /// Defines the index position of the UserDefined command inside the bodyMessage.<p> Value: 13</p>
    /// </summary>
    public static short UserDefinedCommandArrayIndex = 13;

    /// <summary>
    /// Defines the index position from an usable data starts inside the buffer. Value: 18
    /// </summary>
    public static short UserDefinedMessageDataStartIndex = (short)(GameMessage.UserDefinedCommandArrayIndex + 5);
    /// <summary>
    /// Creates a new GameMessage instance, setting it as a User command type, allowing it to be bypassed by the underlaying layer.
    /// </summary>
    /// <param name="kspCommand">Byte parameter allowing to cast it to whatever Enum:Byte type you need.</param>
    /// <param name="messageOwner">Network entity who has control over this message.</param>
    public GameMessage(NetworkEntity messageOwner)
        : base(CommandType.User, messageOwner)
    {
    }

    /// <summary>
    /// Creates an Empty reference to a GameMessage.
    /// </summary>
    /// <returns></returns>
    public override Message Empty()
    {
        return new GameMessage(null);
    }

    /// <summary>
    /// Disposes the message.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
    }

    /// <summary>
    /// Releases everything.
    /// </summary>
    public override void Release()
    {
        base.Release();
    }

    /// <summary>
    /// Creates a null message, being an example about how to create UserCommands.
    /// </summary>
    /// <param name="sender">Network entity who is the owner of this message.</param>
    /// <param name="messageTargets">Those ids that will receive the message. <b>Set -1 to create a full broadcast.</b></param>
    /// <param name="targetMessage">Out reference to the Message to be sent.</param>
    /// <returns></returns>
    public static byte NullMessage(NetworkEntity sender, int messageTargets, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return (byte)Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)KSPMUnitySystem.GameCommand.Null;
        bytesToSend += 1;

        ///Writing the targets
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(messageTargets), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage(sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return (byte)Error.ErrorType.Ok;
    }

    public static byte SceneChanged(NetworkEntity sender, int messageTargets, int scene,out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return (byte)Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)KSPMUnitySystem.GameCommand.SceneChanged;
        bytesToSend += 1;

        ///Writing the targets
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(messageTargets), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writing the index of the scene.
        rawBuffer[bytesToSend] = (byte)scene;
        bytesToSend += 1;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage(sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return (byte)Error.ErrorType.Ok;
    }

    #region Shipsync

    /// <summary>
    /// Asks to the server for a new Id to be assiged on a new vessel.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static byte RequestVesselId(NetworkEntity sender, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return (byte)Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)KSPMUnitySystem.GameCommand.RequestVesselId;
        bytesToSend += 1;

        ///Writing the targets, using -1 to create a broadcastmessage.
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(-1), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage(sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return (byte)Error.ErrorType.Ok;
    }

    public static byte SendVesselId(NetworkEntity sender, uint vesselId, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return (byte)Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)KSPMUnitySystem.GameCommand.RequestVesselId;
        bytesToSend += 1;

        ///Writing the targets, using -1 to create a broadcastmessage.
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(-1), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writing the generated vessel Id.
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(vesselId), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage(sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return (byte)Error.ErrorType.Ok;
    }

    public static byte PackShip(NetworkEntity sender, int messageTargets, KSPMVessel vesselInfo,System.IO.FileStream shipStream, ref int remainingBytes, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        int packSize;
        int vesselPartIndex;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return (byte)Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)KSPMUnitySystem.GameCommand.ShipSync;
        bytesToSend += 1;

        ///Writing the targets
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(messageTargets), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writing the vessel id.
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(vesselInfo.NetworkId), 0, rawBuffer, bytesToSend, 4);
        bytesToSend += 4;
        vesselPartIndex = bytesToSend;

        ///Left some space to write the vessel part number.
        bytesToSend += 2;

        ///shipStream.Seek(0, System.IO.SeekOrigin.Begin);
        ///30 bytes from the message itself.
        packSize = rawBuffer.Length - ( bytesToSend + EndOfMessageCommand.Length );
        remainingBytes = (int)(shipStream.Length - shipStream.Position);
        vesselInfo.Synchronizator.PacketCounter++;
        //Debug.Log(remainingBytes);
        //Debug.Log(packSize);
        if (packSize <= remainingBytes)
        {
            remainingBytes -= shipStream.Read(rawBuffer, bytesToSend, packSize);
            bytesToSend += packSize;
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(vesselInfo.Synchronizator.PacketCounter), 0, rawBuffer, vesselPartIndex, 2);
            ///remainingBytes -= packSize;
        }
        else
        {
            shipStream.Read(rawBuffer, bytesToSend, remainingBytes);
            bytesToSend += remainingBytes;
            ///Writing the packet couter as a negative value to mark it as the last packet.
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((-vesselInfo.Synchronizator.PacketCounter)), 0, rawBuffer, vesselPartIndex, 2);
            remainingBytes = 0;
        }

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage(sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return (byte)Error.ErrorType.Ok;
    }

    #endregion
}