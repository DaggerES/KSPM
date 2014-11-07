using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common
{
    /// <summary>
    /// Error class that contains Error definitions.
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Error codes used by the system.
        /// </summary>
        public enum ErrorType:byte
        { 
            /// <summary>
            /// Everything is fine.
            /// </summary>
            Ok = 0,
            #region ServerErrors

            /// <summary>
            /// The server can not start by some reason.
            /// </summary>
            ServerUnableToRun,
            #endregion

            #region NetworkErrors
            /// <summary>
            /// The message uses a bad format or there are not enough bytes to compose a valid header.
            /// </summary>
            MessageBadFormat,

            /// <summary>
            /// 
            /// </summary>
            MessageIncompleteBytes,

            /// <summary>
            /// The message is greather than the buffer, it is not possible.
            /// </summary>
            MessageCRCError,

            /// <summary>
            /// Null references is passed to the methods.
            /// </summary>
            MessageInvalidRawBytes,
            #endregion

            #region NetworkEntities
            /// <summary>
            /// A null reference is passed as argument or the entity is not valid.
            /// </summary>
            InvalidNetworkEntity,
            #endregion

            #region ServerSideClient
            /// <summary>
            /// The object created to handle a client on the server side is not able to run.
            /// </summary>
            ServerClientUnableToRun,
            #endregion

            #region Client
            /// <summary>
            /// Client is unable to run by some reason.
            /// </summary>
            ClientUnableToRun,

            /// <summary>
            /// A GameUser is invalid.
            /// </summary>
            ClientInvalidGameUser,

            /// <summary>
            /// The server information is not valid.
            /// </summary>
            ClientInvalidServerInformation,

            /// <summary>
            /// Client is not able to connect to the server.
            /// </summary>
            ClientUnableToConnect,
            #endregion

            #region NAT
            /// <summary>
            /// The assigned address is already used or it has not been freed yet.
            /// </summary>
            NATAdrressInUse,
            #endregion

            #region IO
            /// <summary>
            /// Some file used on the system can not be written.
            /// </summary>
            IOFileCanNotBeWritten,

            /// <summary>
            /// The folder does not exists.
            /// </summary>
            IODirectoryNotFound,
            #endregion

            #region UserErrors
            /// <summary>
            /// Bytes with the user's information are incomplete or it does not exists at all.
            /// </summary>
            UserIncompleteBytes,

            /// <summary>
            /// The username has been encoded using some unsupported format.
            /// </summary>
            UserBadFormatString,

            /// <summary>
            /// 
            /// </summary>
            UserMaxlenghtStringReached,

            /// <summary>
            /// 
            /// </summary>
            InvalidUser,
            #endregion

            #region ByteErrors
            /// <summary>
            /// Byte array is null.
            /// </summary>
            InvalidArray,

            /// <summary>
            /// An error ocurred during some encoding/decoding method.
            /// </summary>
            ByteBadFormat,
            #endregion

            #region Chat
            /// <summary>
            /// An invalid or null reference is passed.
            /// </summary>
            ChatInvalidGroup,

            /// <summary>
            /// Sent when a negative value is used to create the groups.
            /// </summary>
            ChatInvalidAvailableGroups,
            #endregion
        };
    }
}
