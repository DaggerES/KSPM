using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common
{
    public abstract class AbstractSettings
    {
        public abstract void Release();
        //public static abstract void ReadSettings(ref AbstractSettings settings);
        //public static abstract bool WriteSettings(ref AbstractSettings settings);
    }
}
