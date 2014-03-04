using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPM.Network.Server.UserManagement
{
    public class LowlevelUserManagmentSystem : UserManagementSystem
    {
        public override bool Query(ref Common.NetworkEntity entityToValidate)
        {
            return true;
        }
    }
}
