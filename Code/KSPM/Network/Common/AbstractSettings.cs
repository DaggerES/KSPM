using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Common
{
    /// <summary>
    /// Abstrac class to be the base of any settings object used by the system.
    /// </summary>
    public abstract class AbstractSettings
    {
        /// <summary>
        /// Method used to releases each resource consumed by the object.<b>Abstract.</b>
        /// </summary>
        public abstract void Release();
    }
}
