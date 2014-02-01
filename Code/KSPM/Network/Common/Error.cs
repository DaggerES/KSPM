using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common
{
    public class Error
    {
        public enum ErrorType:byte { Ok = 0};
        public enum ServerErrors : byte { ServerUnableToRun = 0 };
    }
}
