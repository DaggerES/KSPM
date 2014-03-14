using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common
{
    public class Error
    {
        public enum ErrorType:byte
        { 
            Ok = 0,
            #region ServerErrors
            ServerUnableToRun,
            #endregion

            #region NetworkErrors
            MessageBadFormat,
            MessageIncompleteBytes,
            MessageCRCError,
            MessageInvalidRawBytes,
            #endregion

            #region NetworkEntities
            InvalidNetworkEntity,
            #endregion

            #region ServerSideClient
            ServerClientUnableToRun,
            #endregion

            #region Client
            ClientUnableToRun,
            ClientInvalidGameUser,
            ClientInvalidServerInformation,
            ClientUnableToConnect,
            #endregion

            #region NAT
            NATAdrressInUse,
            #endregion

            #region Settings
            IOFileCanNotBeWritten,
            #endregion

            #region UserErrors
            UserIncompleteBytes,
            UserBadFormatString,
            UserMaxlenghtStringReached,
            InvalidUser,
            #endregion

            #region ByteErrors
            InvalidArray,
            ByteBadFormat,
            #endregion

            #region Chat
            ChatInvalidGroup,
            ChatInvalidAvailableGroups,
            #endregion
        };
    }
}
