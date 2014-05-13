using UnityEngine;
using System.Collections;

using KSPM.Network.Common.Messages;
using KSPM.Network.Common;
using KSPM.Network.Server;

public class GameMessage : BufferedMessage
{
    public enum GameCommand : byte
    {
        Null = 0,
        UserConnected,
        UserDisconnected,
    }

    #region StaticMethods

    /// <summary>
    /// Follows this example to create your own message commands.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static Error.ErrorType UserDefinedNullMessage(NetworkEntity sender, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 4;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.Null;
        bytesToSend += 1;


        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new ManagedMessage((CommandType)rawBuffer[Message.HeaderOfMessageCommand.Length + 4], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Creates a user connected message. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static Error.ErrorType UserConnectedMessage(NetworkEntity sender, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 4;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.UserConnected;
        bytesToSend += 1;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new ManagedMessage((CommandType)rawBuffer[Message.HeaderOfMessageCommand.Length + 4], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    #endregion
}
