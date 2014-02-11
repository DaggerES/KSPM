namespace KSPM.Game
{
    /// <summary>
    /// Represents a game user, it  is possible to use the hash to identify those ships that belongs to this user.
    /// </summary>
    public class GameUser : User
    {
        /// <summary>
        /// Creates a GameUser object.
        /// </summary>
        /// <param name="userHash">Hash of the user.</param>
        public GameUser(ref string userHash)
            : base(ref userHash)
        {
        }

        public GameUser(ref string username, ref string userHash)
            : base(ref userHash)
        {
            this.username = username;
        }
    }
}
