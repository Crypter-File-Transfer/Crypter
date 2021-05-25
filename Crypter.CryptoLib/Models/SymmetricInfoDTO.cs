using System;

namespace Crypter.CryptoLib.Models
{
    public class SymmetricInfoDTO
    {
        public byte[] Key { get; }
        public byte[] IV { get; }

        /// <summary>
        /// Create a new signature from params
        /// </summary>
        /// <param name="symmetricKey"></param>
        /// <param name="symmetricIV"></param>
        public SymmetricInfoDTO(byte[] symmetricKey, byte[] symmetricIV)
        {
            Key = symmetricKey;
            IV = symmetricIV;
        }

        /// <summary>
        /// Create a new signature from a symmetricInfo string
        /// </summary>
        /// <exception cref="FormatException"></exception>
        /// <returns></returns>
        public SymmetricInfoDTO(string symmetricInfo)
        {
            var pieces = symmetricInfo.Split('\n');
            if (pieces.Length != 2)
            {
                throw new FormatException("Cannot instantiate from provided string");
            }

            Key = Convert.FromBase64String(pieces[0]);
            IV = Convert.FromBase64String(pieces[1]);
        }

        /// <summary>
        /// Serialize an AnonymousSignature
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Convert.ToBase64String(Key)}\n{Convert.ToBase64String(IV)}";
        }
    }
}
