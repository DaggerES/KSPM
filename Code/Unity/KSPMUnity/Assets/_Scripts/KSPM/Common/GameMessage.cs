using UnityEngine;
using System.Collections;

using KSPM.Network.Common.Packet;
using KSPM.Network.Common.Messages;
using KSPM.Network.Common;
using KSPM.Network.Server;

/// <summary>
/// GameMessage used to be sent through the TCP socket<b>THIS MESSAGE CAN NOT BE SENT USING THE UDP SOCKET.</b>.
/// 5th position is where the user's defined command is placed inside the body message. BE CAREFUL THAT THIS IS 0-BASED.
/// </summary>
public class GameMessage : ManagedMessage
{
    public enum GameCommand : byte
    {
        Null = 0,
        UserConnected,
        UserDisconnected,
        UserGameParameters,

        GameStatus,
        GameParameters,
    }

    /// <summary>
    /// Tells what kind of message it is.
    /// </summary>
    protected GameCommand gameCommand;

    /// <summary>
    /// Creates a new GameMessage instance, setting it as a User command type, allowing it to be bypassed by the underlaying layer.
    /// </summary>
    /// <param name="commandType">GameCommand parameter to say what kind of message is.</param>
    /// <param name="messageOwner">Network entity who has control over this message.</param>
    public GameMessage(GameCommand commandType, NetworkEntity messageOwner)
        : base(CommandType.User, messageOwner)
    {
        this.gameCommand = commandType;
    }

    public override Message Empty()
    {
        return new GameMessage(GameCommand.Null, null);
    }

    public override void Dispose()
    {
        this.gameCommand = GameCommand.Null;
        base.Dispose();
    }

    public override void Release()
    {
        this.gameCommand = GameCommand.Null;
        base.Release();
    }

    #region Setters/Getters

    /// <summary>
    /// Gets the user defined command.
    /// </summary>
    public GameCommand UserCommand
    {
        get
        {
            return this.gameCommand;
        }
    }

    #endregion

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
        ///5th position is where the user's defined command is placed inside the body message. BE CAREFUL THAT THIS IS 0-BASED.
        targetMessage = new GameMessage((GameCommand)rawBuffer[Message.HeaderOfMessageCommand.Length + 5], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Creates a user connected message. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static Error.ErrorType UserConnectedMessage(NetworkEntity sender, GamePlayer playerGameObject,out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        byte[] byteBuffer;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.UserConnected;
        bytesToSend += 1;

        ///Writing the Player GameId.
        //player = playerGameObject.GetComponent<MPGamePlayer>();
        byteBuffer = System.BitConverter.GetBytes(playerGameObject.GameId);
        System.Buffer.BlockCopy(byteBuffer, 0, rawBuffer, bytesToSend, byteBuffer.Length);
        bytesToSend += byteBuffer.Length;

        rawBuffer[bytesToSend] = (byte)playerGameObject.GamingRol;
        bytesToSend += 1;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage((GameCommand)rawBuffer[Message.HeaderOfMessageCommand.Length + 9], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Creates a user connected message. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static Error.ErrorType UserGameParametersMessage(NetworkEntity sender, GamePlayer playerGameObject, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        byte[] byteBuffer;
        GamePlayer player = null;

        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.UserGameParameters;
        bytesToSend += 1;

        ///Writing the Player GameId.
        //player = playerGameObject.GetComponent<MPGamePlayer>();
        byteBuffer = System.BitConverter.GetBytes(playerGameObject.GameId);
        System.Buffer.BlockCopy(byteBuffer, 0, rawBuffer, bytesToSend, byteBuffer.Length);
        bytesToSend += byteBuffer.Length;

        ///Writing the Player gaming rol.
        rawBuffer[bytesToSend] = (byte)playerGameObject.GamingRol;
        bytesToSend += 1;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage((GameCommand)rawBuffer[Message.HeaderOfMessageCommand.Length + 9], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    public static Error.ErrorType GameStartParametersMessage(NetworkEntity sender, System.Collections.Generic.List<GameObject> players, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        byte[] byteBuffer;

        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        if (players == null)
        {
            return Error.ErrorType.InvalidArray;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.GameParameters;
        bytesToSend += 1;

        ///Writing the users information.
        ///Writing how many users are connected.
        byteBuffer = System.BitConverter.GetBytes(players.Count);
        System.Buffer.BlockCopy(byteBuffer, 0, rawBuffer, bytesToSend, byteBuffer.Length);
        bytesToSend += byteBuffer.Length;

        for (int i = 0; i < players.Count; i++)
        {
            ///Writing the player GameId.
            byteBuffer = System.BitConverter.GetBytes(players[ i ].GetComponent<GamePlayer>().GameId);
            System.Buffer.BlockCopy(byteBuffer, 0, rawBuffer, bytesToSend, byteBuffer.Length);
            bytesToSend += byteBuffer.Length;

            ///Writing the player gaming rol.
            rawBuffer[bytesToSend] = (byte)players[i].GetComponent<GamePlayer>().GamingRol;
            bytesToSend += 1;
        }

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage((GameCommand)rawBuffer[Message.HeaderOfMessageCommand.Length + 9], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Creates a user disconnected message.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static Error.ErrorType UserDisconnectedMessage(NetworkEntity sender,int playerId, out Message targetMessage)
    {
        int bytesToSend = Message.HeaderOfMessageCommand.Length;
        byte[] rawBuffer = new byte[ServerSettings.ServerBufferSize];
        targetMessage = null;
        byte[] messageHeaderContent = null;
        byte[] byteBuffer;
        if (sender == null)
        {
            return Error.ErrorType.InvalidNetworkEntity;
        }

        ///Writing header
        System.Buffer.BlockCopy(Message.HeaderOfMessageCommand, 0, rawBuffer, 0, Message.HeaderOfMessageCommand.Length);
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.UserDisconnected;
        bytesToSend += 1;

        byteBuffer = System.BitConverter.GetBytes(playerId);
        System.Buffer.BlockCopy(byteBuffer, 0, rawBuffer, bytesToSend, byteBuffer.Length);
        bytesToSend += byteBuffer.Length;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage((GameCommand)rawBuffer[Message.HeaderOfMessageCommand.Length + 9], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }

    /// <summary>
    /// Creates a user disconnected message.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetMessage"></param>
    /// <returns></returns>
    public static Error.ErrorType GameStatusMessage(NetworkEntity sender, GameManager game, out Message targetMessage)
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
        bytesToSend += 8;

        ///Writing the command.
        rawBuffer[bytesToSend] = (byte)Message.CommandType.User;
        bytesToSend += 1;

        ///Write your own data here, settting the proper command and before setting the EndOfMessageCommand.

        ///Writing the user-s defined command.
        rawBuffer[bytesToSend] = (byte)GameCommand.GameStatus;
        bytesToSend += 1;

        rawBuffer[bytesToSend] = (byte)game.currentStatus;
        bytesToSend += 1;

        ///Writing the EndOfMessageCommand.
        System.Buffer.BlockCopy(Message.EndOfMessageCommand, 0, rawBuffer, bytesToSend, Message.EndOfMessageCommand.Length);
        bytesToSend += EndOfMessageCommand.Length;

        ///Writing the message length.
        messageHeaderContent = System.BitConverter.GetBytes(bytesToSend);
        System.Buffer.BlockCopy(messageHeaderContent, 0, rawBuffer, Message.HeaderOfMessageCommand.Length, messageHeaderContent.Length);

        ///Creating the Message
        targetMessage = new GameMessage((GameCommand)rawBuffer[Message.HeaderOfMessageCommand.Length + 9], sender);
        targetMessage.SetBodyMessageNoClone(rawBuffer, (uint)bytesToSend);
        return Error.ErrorType.Ok;
    }



    public static GameError.GameErrorType LoadFromMessage(out Message targetMessage, Message srcMessage)
    {
        targetMessage = null;
        if (srcMessage == null)
        {
            return GameError.GameErrorType.MessageNullSourceMessage;
        }
        if (srcMessage.Command != CommandType.User)
        {
            return GameError.GameErrorType.MessageInvalidSourceMessage;
        }
        srcMessage.UserDefinedCommand = srcMessage.bodyMessage[PacketHandler.PrefixSize + 4 + 1];
        targetMessage = new GameMessage((GameCommand)srcMessage.UserDefinedCommand, ((ManagedMessage)srcMessage).OwnerNetworkEntity);
        ((GameMessage)targetMessage).SetBodyMessageNoClone(srcMessage.bodyMessage, srcMessage.MessageBytesSize);
        return GameError.GameErrorType.Ok;
    }

    #endregion
}
