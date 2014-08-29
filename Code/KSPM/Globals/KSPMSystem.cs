namespace KSPM.Globals
{
    public abstract class KSPMSystem
    {
        /// <summary>
        /// System priority levels.
        /// </summary>
        public enum PriorityLevel : byte
        {
            /// <summary>
            /// High priority -> Connection commands.
            /// </summary>
            Critical = 0,

            /// <summary>
            /// User commands. These commands are passed to the upside level.
            /// </summary>
            High,

            /// <summary>
            /// At this moment this level has no commands.
            /// </summary>
            Medium,

            /// <summary>
            /// Chat commands, the lowest priority on the system.
            /// </summary>
            Lowest,

            /// <summary>
            /// Means that this level is going to be droped.
            /// </summary>
            Disposable = 254,
        };

        public enum WarningLevel:byte
        {
            /// <summary>
            /// Everything is fine.
            /// </summary>
            None = 0,

            /// <summary>
            /// Keep an eye on it.
            /// </summary>
            Easy,

            /// <summary>
            /// Starting to bypass some packets.
            /// </summary>
            Carefull,

            /// <summary>
            /// A queue is overflown, backup is used.
            /// </summary>
            Warning,

            /// <summary>
            /// The system is totally overflown so it must be purged, start to drop packets.
            /// </summary>
            Halt,
        }
    }
}
