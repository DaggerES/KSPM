using System.Security.Cryptography;

namespace KSPM.IO.Security
{
    /// <summary>
    /// Class to compute hashes., by default it uses the SHA512 method.
    /// </summary>
    public class Hash
    {
        /// <summary>
        /// Enum to define which method will be used to compute the hashes.
        /// </summary>
        public enum HashingAlgorithm:byte
        { 
            /// <summary>
            /// Uses a SHA with 512 bits.
            /// </summary>
            SHA512 = 0, 

            /// <summary>
            /// Uses MD5.
            /// </summary>
            MD5,

            /// <summary>
            /// Uses HMAC with SHA 512, allowing to set a key to generate the hash.
            /// </summary>
            HMAC_SHA512,
        };

        /// <summary>
        /// Static reference to the HashAlgorith.
        /// </summary>
        protected static HashAlgorithm Hasher = Hash.Hasher = HMACSHA512.Create("HMACSHA512");

        /// <summary>
        /// Holds the algorithm used to compute the hashes.
        /// </summary>
        protected static HashingAlgorithm Algorithm = HashingAlgorithm.HMAC_SHA512;

        /// <summary>
        /// Initializes the hasher reference to a new one.
        /// </summary>
        /// <param name="algorithm">The algorithm that should be used.</param>
        public static void Initialize(HashingAlgorithm algorithm)
        {
            Hash.Algorithm = algorithm;
            switch (algorithm)
            {
                case HashingAlgorithm.MD5:
                    Hash.Hasher.Clear();
                    Hash.Hasher = MD5.Create();
                    break;
                case HashingAlgorithm.HMAC_SHA512:
                    Hash.Hasher = HMACSHA512.Create("HMACSHA512");
                    break;
                case HashingAlgorithm.SHA512:
                default:
                    Hash.Hasher.Clear();
                    Hash.Hasher = SHA512.Create();
                    break;
            }
        }

        /// <summary>
        /// Computes a hash from the given byte array and write it down into the hashedBytes param.
        /// </summary>
        /// <param name="rawBytes">Byte array that would be hashed.</param>
        /// <param name="offset">The offset to take in count to perform the hash, so not the entire array would be hashed.</param>
        /// <param name="bytesToHash">How many bytes must be read by the hasher.</param>
        /// <param name="hashedBytes">Out reference to the hashed bytes.</param>
        /// <returns></returns>
        public static KSPM.Network.Common.Error.ErrorType GetHash(ref byte[] rawBytes, uint offset, uint bytesToHash, out byte[] hashedBytes)
        {
            hashedBytes = null;
            if (rawBytes == null)
            {
                return KSPM.Network.Common.Error.ErrorType.InvalidArray;
            }
            if (bytesToHash + offset > rawBytes.Length)
            {
                bytesToHash = (uint)rawBytes.Length - offset;
            }
            try
            {
                hashedBytes = Hasher.ComputeHash(rawBytes, (int)offset, (int)bytesToHash);
            }
            catch (System.ObjectDisposedException)
            {
                return Network.Common.Error.ErrorType.InvalidArray;
            }
            catch (System.NullReferenceException)
            {
                return Network.Common.Error.ErrorType.InvalidArray;
            }
            return Network.Common.Error.ErrorType.Ok;
        }

        /// <summary>
        /// Generates a hash using the email and the password to generate a key and then computes the hash on them. Requires that the hasher must be initialized to HMAC_SHA512
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static KSPM.Network.Common.Error.ErrorType GenerateHashToUser( string email, string password, ref byte[] hash, ref int hashSize)
        {
            HMACSHA512 hmac;
            byte[] emailRaw, passwordRaw, key, generatedHash;
            if( Hash.Algorithm != HashingAlgorithm.HMAC_SHA512)
            {
                KSPM.Globals.KSPMGlobals.Globals.Log.WriteTo(string.Format("Hasher is not set to HMAC_SHA512, current {0}. Changing it to HMAC_SHA512", Hash.Algorithm.ToString()));
                Hash.Initialize(HashingAlgorithm.HMAC_SHA512);
            }
            hashSize = 0;
            emailRaw = System.Text.UTF8Encoding.UTF8.GetBytes(email);
            passwordRaw = System.Text.UTF8Encoding.UTF8.GetBytes(password);
            key = new byte[emailRaw.Length + passwordRaw.Length];
            System.Buffer.BlockCopy(emailRaw, 0, key, 0, emailRaw.Length);
            System.Buffer.BlockCopy(passwordRaw, 0, key, emailRaw.Length, passwordRaw.Length);
            hmac = (HMACSHA512)Hash.Hasher;
            hmac.Key = key;
            generatedHash = hmac.ComputeHash(key);
            if( generatedHash.Length > hash.Length)
            {
                System.Buffer.BlockCopy(generatedHash, 0, hash, 0, hash.Length);
                hashSize = hash.Length;
            }
            else
            {
                System.Buffer.BlockCopy(generatedHash, 0, hash, 0, generatedHash.Length);
                hashSize = generatedHash.Length;
            }
            return Network.Common.Error.ErrorType.Ok;
        }
    }
}
