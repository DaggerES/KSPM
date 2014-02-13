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
        public enum HashingAlgorithm:byte { SHA512 = 0, MD5 };

        /// <summary>
        /// Static reference to the HashAlgorith.
        /// </summary>
        protected static HashAlgorithm Hasher = SHA512.Create();

        /// <summary>
        /// Initializes the hasher reference to a new one.
        /// </summary>
        /// <param name="algorithm">The algorithm that should be used.</param>
        public static void Initialize(HashingAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case HashingAlgorithm.MD5:
                    Hash.Hasher.Clear();
                    Hash.Hasher = MD5.Create();
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
    }
}
