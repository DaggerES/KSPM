namespace KSPM.Game
{
    /// <summary>
    /// Represents a game user, it  is possible to use the hash to identify those ships that belongs to this user.
    /// </summary>
    public class GameUser : User
    {
        /// <summary>
        /// Cyclical reference to the network entity who owns this GameUser.
        /// </summary>
        protected Network.Common.NetworkEntity parent;

        /// <summary>
        /// Creates a GameUser object.
        /// </summary>
        /// <param name="userHash">Hash of the user.</param>
        public GameUser(ref byte[] userHash)
            : base(ref userHash)
        {
            this.parent = null;
        }

        /// <summary>
        /// Creates a new reference of a GameUser.
        /// </summary>
        /// <param name="username">Username of the reference.</param>
        /// <param name="userHash">Byte array to be set as hash of the GameUser</param>
        public GameUser(ref string username, ref byte[] userHash)
            : base(ref userHash)
        {
            this.username = username;
            this.parent = null;
        }

        /// <summary>
        /// Sets/Gets the parent of this GameUser.
        /// </summary>
        public Network.Common.NetworkEntity Parent
        {
            get
            {
                return this.parent;
            }
            set
            {
                this.parent = value;
            }
        }
    }
}
