using KSPM.Network.Common;
using System.Text;

namespace KSPM.Game
{
    /// <summary>
    /// A representation of an user of the game.
    /// </summary>
    public abstract class User
    {
        /// <summary>
        /// UTF-8 String encoder/decoder.
        /// </summary>
        protected static readonly UTF8Encoding Encoder = new UTF8Encoding();

        /// <summary>
        /// A references countr, its purpose is to have a control of how many User objects have been created, allowing to set it as a ID.
        /// </summary>
        protected static int UserReferencesCounter = 1;

        /// <summary>
        /// Username, supporting UTF-8 encoding on it.
        /// </summary>
        protected string username;

        /// <summary>
        /// The hash of this user, it should be unique.***I have to find the way to achieve this.
        /// </summary>
        protected string hash;

        /// <summary>
        /// Unique Id
        /// </summary>
        protected int id;

        /// <summary>
        /// Tries to convert the amount of bytes especified by the bytesToRead argument to an UTF-8 encoded string.
        /// <b>Note:</b> If the offset + bytesToRead is greather than the array's lenght the amount of read bytes would be truncated.
        /// </summary>
        /// <param name="rawBytes">Reference to the byte array in raw format.</param>
        /// <param name="offset">Position to start to read the bytes.</param>
        /// <param name="bytesToRead">How many bytes the method should read.</param>
        /// <param name="username">Out reference to the string which would hold the converted string.</param>
        /// <returns>Ok if no errors ocurred.</returns>
        public static Error.ErrorType DecodeUsernameFromBytes(ref byte[] rawBytes, uint offset, uint bytesToRead, out string username)
        {
            username = null;
            if (rawBytes == null)
            {
                return Error.ErrorType.UserIncompleteBytes;
            }
            if ( offset <= rawBytes.Length &&  offset + bytesToRead > rawBytes.Length)
            {
                bytesToRead = (uint)rawBytes.Length - offset;
            }
            try
            {
                username = User.Encoder.GetString(rawBytes, (int)offset, (int)bytesToRead);
            }
            catch (System.Text.DecoderFallbackException)
            {
                return Error.ErrorType.UserBadFormatString;
            }
            catch (System.ArgumentException)
            {
                return Error.ErrorType.UserBadFormatString;
            }
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Tries to encode the string into a bytes array.
        /// </summary>
        /// <param name="username">Reference to the string that should be converted.</param>
        /// <param name="maxBytesLenght">Max allowed lenght to the byte array.</param>
        /// <param name="rawBytes">Out reference to the byte in raw format.</param>
        /// <returns></returns>
        public static Error.ErrorType EncodeUsernameToBytes( ref string username, out byte[] rawBytes )
        {
            rawBytes = null;
            if( username == null )
            {
                return Error.ErrorType.UserBadFormatString;
            }
            rawBytes = User.Encoder.GetBytes(username);
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Contructor.
        /// </summary>
        /// <param name="hashCode"></param>
        public User(ref string hashCode)
        {
            this.hash = hashCode;
            this.id = User.UserReferencesCounter++;
            this.username = string.Format("GameUser-{0}", this.id);
        }

        public string Username
        {
            get
            {
                return this.username;
            }
            set
            {
                this.username = value;
            }
        }

        public string Hash
        {
            get
            {
                return this.hash;
            }
        }
    }
}
