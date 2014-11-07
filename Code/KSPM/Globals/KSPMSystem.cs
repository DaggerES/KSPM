namespace KSPM.Globals
{
    /// <summary>
    /// Class to define some properties of the KSPM system.
    /// </summary>
    public abstract class KSPMSystem
    {
        /// <summary>
        /// System priority levels.
        /// </summary>
        public enum PriorityLevel : byte
        {
            /// <summary>
            /// Critical priority -> Connection commands. Byte:0
            /// </summary>
            Critical = 0,

            /// <summary>
            /// High priority -> User commands. These commands are passed to the upside level. Byte:1
            /// </summary>
            High,

            /// <summary>
            /// At this moment this level has no commands. Byte:2
            /// </summary>
            Medium,

            /// <summary>
            /// Chat commands, the lowest priority on the system. Byte:3
            /// </summary>
            Lowest,

            /// <summary>
            /// Means that this level is going to be droped.
            /// </summary>
            Disposable = 254,
        };

        /// <summary>
        /// Defines the warning levels of the KSPM system.
        /// </summary>
        public enum WarningLevel:byte
        {
            /// <summary>
            /// Everything is fine. Byte:0
            /// </summary>
            None = 0,

            /// <summary>
            /// Keep an eye on it. Byte:1
            /// </summary>
            Easy,

            /// <summary>
            /// Starting to bypass some packets. Byte:2
            /// </summary>
            Carefull,

            /// <summary>
            /// A queue is overflown, backup is used. Byte:3
            /// </summary>
            Warning,

            /// <summary>
            /// The system is totally overflown so it must be purged, start to drop packets. Byte:4
            /// </summary>
            Halt,
        }
    }
}
