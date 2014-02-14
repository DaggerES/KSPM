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
        /// The hash of this user in a human understable format, it should be unique.***I have to find the way to achieve this.
        /// </summary>
        protected string humanHash;

        /// <summary>
        /// Binary hash of the user.
        /// </summary>
        protected byte[] hash;

        /// <summary>
        /// Unique Id
        /// </summary>
        protected int id;

        /// <summary>
        /// Counts how many times this user have tried to authenticate into the server.
        /// </summary>
        protected int authenticationAttempts;

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
        /// Tries to create an GameUser reference and fill it with the data contained in raw format.
        /// </summary>
        /// <param name="rawBytes">Byte array in raw format.</param>
        /// <param name="targetUser">Out reference to the GameUser.</param>
        /// <returns></returns>
        public static Error.ErrorType InflateUserFromBytes(ref byte[] rawBytes, out GameUser targetUser)
        {
            targetUser = null;
            byte[] hashCode =null;
            short hashSize;
            string buffer;
            if (rawBytes == null)
            {
                return Error.ErrorType.UserIncompleteBytes;
            }
            ///6 because in that position is where the username's bytes start, so there is the add of username's length plus the 6th position.
            hashSize = System.BitConverter.ToInt16(rawBytes, 6 + (int)rawBytes[5]);
            hashCode = new byte[hashSize];
            ///8 because is the 6th position + 2 bytes of the hashsize's bytes.
            System.Buffer.BlockCopy(rawBytes, 8 + (int)rawBytes[5], hashCode, 0, hashSize);

            if (User.DecodeUsernameFromBytes(ref rawBytes, 6, (uint)rawBytes[5], out buffer) == Error.ErrorType.Ok)
            {
                targetUser = new GameUser(ref buffer, ref hashCode);
            }
            return Error.ErrorType.Ok;
        }

        /// <summary>
        /// Contructor.
        /// </summary>
        /// <param name="hashCode"></param>
        public User(ref byte[] hashCode)
        {
            this.hash = hashCode;
            this.id = User.UserReferencesCounter++;
            this.username = string.Format("GameUser-{0}", this.id);
        }

        /// <summary>
        /// Sets/gets the username of the user, <b>this is not a PK</b>, be careful about this.
        /// </summary>
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

        /// <summary>
        /// Returns the user's hash.
        /// </summary>
        public byte[] Hash
        {
            get
            {
                return this.hash;
            }
        }

        /// <summary>
        /// Sets/gets the authentication attempts counter.
        /// </summary>
        public int AuthencticationAttempts
        {
            get
            {
                return this.authenticationAttempts;
            }
            set
            {
                this.authenticationAttempts = value;
            }
        }
    }
}
