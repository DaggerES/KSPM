using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common;
using KSPM.Network.Server;
using KSPM.Network.Client;

public class UDPGameMessage
{
    public enum UDPGameCommand : byte
    {
        Null = 0,
        BallUpdate,
        BallForce,
        ControlUpdate,
    }


    /// <summary>
    /// Writes an UDPParingMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
    /// </summary>
    /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
    /// <param name="targetMessage">Out reference to the Message object to be created.</param>
    /// <returns></returns>
    public static Error.ErrorType LoadUDPUpdateBallMessage(NetworkEntity sender, MovementManager movement, UnityEngine.Vector3 data, ref Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        ServerSideClient ssClientReference = (ServerSideClient)sender;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, ssClientReference.udpCollection.rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 4;///4 bytes reserver to the message length.

        ///Writing the Command byte.
        ssClientReference.udpCollection.rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ssClientReference.udpCollection.rawBuffer[bytesToSend] = (byte)UDPGameCommand.BallUpdate;
        bytesToSend += 1;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(data.x), 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, 4);
        bytesToSend += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(data.y), 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, 4);
        bytesToSend += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(data.z), 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writint the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += Message.EndOfMessageCommand.Length;

        ///Writint the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, ssClientReference.udpCollection.rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        System.Buffer.BlockCopy(ssClientReference.udpCollection.rawBuffer, 0, targetMessage.bodyMessage, 0, bytesToSend);
        targetMessage.MessageBytesSize = (uint)bytesToSend;
        ((RawMessage)targetMessage).ReallocateCommand();
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Writes an UDPParingMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
    /// </summary>
    /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
    /// <param name="targetMessage">Out reference to the Message object to be created.</param>
    /// <returns></returns>
    public static Error.ErrorType LoadUDPBallForceMessage(NetworkEntity sender, MovementManager movement, ref Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        ServerSideClient ssClientReference = (ServerSideClient)sender;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, ssClientReference.udpCollection.rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 4;///4 bytes reserver to the message length.

        ///Writing the Command byte.
        ssClientReference.udpCollection.rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ssClientReference.udpCollection.rawBuffer[bytesToSend] = (byte)UDPGameCommand.BallForce;
        bytesToSend += 1;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(movement.force.x), 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, 4);
        bytesToSend += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(movement.force.y), 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, 4);
        bytesToSend += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(movement.force.z), 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, 4);
        bytesToSend += 4;

        ///Writint the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, ssClientReference.udpCollection.rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += Message.EndOfMessageCommand.Length;

        ///Writint the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, ssClientReference.udpCollection.rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        System.Buffer.BlockCopy(ssClientReference.udpCollection.rawBuffer, 0, targetMessage.bodyMessage, 0, bytesToSend);
        targetMessage.MessageBytesSize = (uint)bytesToSend;
        ((RawMessage)targetMessage).ReallocateCommand();
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Writes an UDPParingMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
    /// </summary>
    /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
    /// <param name="targetMessage">Out reference to the Message object to be created.</param>
    /// <returns></returns>
    public static Error.ErrorType LoadUDPControlUpdateMessage(NetworkEntity sender, UnityEngine.Vector3 displacement, ref Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, targetMessage.bodyMessage, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 4;///4 bytes reserver to the message length.

        ///Writing the Command byte.
        targetMessage.bodyMessage[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        targetMessage.bodyMessage[bytesToSend] = (byte)UDPGameCommand.ControlUpdate;
        bytesToSend += 1;

        System.Buffer.BlockCopy(System.BitConverter.GetBytes(displacement.x), 0, targetMessage.bodyMessage, bytesToSend, 4);
        bytesToSend += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(displacement.y), 0, targetMessage.bodyMessage, bytesToSend, 4);
        bytesToSend += 4;
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(displacement.z), 0, targetMessage.bodyMessage, bytesToSend, 4);
        bytesToSend += 4;

        ///Writint the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, targetMessage.bodyMessage, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += Message.EndOfMessageCommand.Length;

        ///Writint the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, targetMessage.bodyMessage, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        //System.Buffer.BlockCopy(targetMessage.bodyMessage, 0, targetMessage.bodyMessage, 0, bytesToSend);
        targetMessage.MessageBytesSize = (uint)bytesToSend;
        ((RawMessage)targetMessage).ReallocateCommand();
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Writes an UDPParingMessage message in a raw format into the sender's udp buffer then creates a Message object. <b>The previous content is discarded.</b>
    /// </summary>
    /// <param name="sender">Reference to sender that holds the buffer to write in.</param>
    /// <param name="targetMessage">Out reference to the Message object to be created.</param>
    /// <returns></returns>
    public static Error.ErrorType LoadUDPControlUpdateMessage(NetworkEntity sender, UserHostControl.MovementAction actionToDo, ref Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, targetMessage.bodyMessage, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 4;///4 bytes reserver to the message length.

        ///Writing the Command byte.
        targetMessage.bodyMessage[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        targetMessage.bodyMessage[bytesToSend] = (byte)UDPGameCommand.ControlUpdate;
        bytesToSend += 1;

        targetMessage.bodyMessage[bytesToSend] = (byte)actionToDo;
        bytesToSend += 1;

        ///Writint the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, targetMessage.bodyMessage, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += Message.EndOfMessageCommand.Length;

        ///Writint the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, targetMessage.bodyMessage, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        //System.Buffer.BlockCopy(targetMessage.bodyMessage, 0, targetMessage.bodyMessage, 0, bytesToSend);
        targetMessage.MessageBytesSize = (uint)bytesToSend;
        ((RawMessage)targetMessage).ReallocateCommand();
        return Error.ErrorType.Ok;
    }
}
