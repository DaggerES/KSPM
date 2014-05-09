namespace KSPM.Network.Server
{
    /// <summary>
    /// Provides a pool of free port numbers, with a closed set given by [startAssignationPort, endAssignationPort]
    /// </summary>
    public class IOPortManager
    {
        public enum PortProtocool:byte
        {
            None = 0,
            UDP,
            TCP
        };

        /// <summary>
        /// Tells where the assignation table starts.
        /// </summary>
        protected int assignablePortNumberStart;

        /// <summary>
        /// Tells where the assignation table ends.
        /// </summary>
        protected int assignablePortNumberEnd;

        /// <summary>
        /// Dictionary to keep track of the assigned ports.
        /// </summary>
        protected System.Collections.Generic.Dictionary<int, PortProtocool> assignedPorts;

        /// <summary>
        /// Queue to hold all the available ports.
        /// </summary>
        protected System.Collections.Generic.Queue<int> freePorts;

        /// <summary>
        /// Creates an instance of the IOPortManager.
        /// </summary>
        /// <param name="startAssignationPort">Where the assignation should start at.</param>
        /// <param name="endAssignationPort">Where the assignation should end at<b>Set 0 to use the MaxPort available (65535)</b></param>
        public IOPortManager(int startAssignationPort, int endAssignationPort)
        {
            this.assignablePortNumberStart = startAssignationPort;
            this.assignablePortNumberEnd = (endAssignationPort == 0) ? System.Net.IPEndPoint.MaxPort : endAssignationPort;
            this.assignedPorts = new System.Collections.Generic.Dictionary<int, PortProtocool>();
            this.freePorts = new System.Collections.Generic.Queue<int>(this.assignablePortNumberEnd - this.assignablePortNumberStart + 1);
            for (int i = this.assignablePortNumberStart; i <= this.assignablePortNumberEnd; i++)
            {
                this.freePorts.Enqueue(i);
            }
        }

        /// <summary>
        /// Gets the next available port between the set established at the beginning.
        /// </summary>
        /// <param name="usedProtocool"></param>
        /// <returns></returns>
        public int NextPort(PortProtocool usedProtocool)
        {
            int port = -1;
            lock (this.freePorts)
            {
                if (this.freePorts.Count > 0)
                {
                    port = this.freePorts.Dequeue();
                }
            }
            if (port > 0)
            {
                this.assignedPorts.Add(port, usedProtocool);
            }
            return port;
        }

        /// <summary>
        /// Recycles a port placing it again in the available ports structure.
        /// </summary>
        /// <param name="port"></param>
        public void RecyclePort(int port)
        {
            if (this.assignedPorts.ContainsKey(port))
            {
                lock (this.freePorts)
                {
                    this.freePorts.Enqueue(port);
                }
                this.assignedPorts.Remove(port);
            }
        }

        /// <summary>
        /// Releases all the used resources by the IOPortManager.
        /// </summary>
        public void Release()
        {
            this.freePorts.Clear();
            this.freePorts = null;
            this.assignedPorts.Clear();
            this.assignedPorts = null;
            this.assignablePortNumberStart = this.assignablePortNumberEnd = 0;
        }
    }
}
