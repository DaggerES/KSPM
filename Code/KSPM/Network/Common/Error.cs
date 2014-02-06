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
            #endregion

            #region NetworkEntities
            InvalidNetworkEntity,
            #endregion

            #region ServerSideClient
            ServerClientUnableToRun
            #endregion
        };
    }
}
