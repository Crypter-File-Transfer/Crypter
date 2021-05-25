using System;
using System.IO;
using System.Text;
using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
using Crypter.CryptoLib.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace Crypter.CryptoLib
{
    public static class Common
    {
        /// <summary>
        /// Create new 'SymmetricCryptoParams' instance of the desired 'strength'
        /// </summary>
        /// <param name="strength"></param>
        /// <returns>A 'SymmetricCryptoParams' instance, which contains a key and IV</returns>
        public static SymmetricCryptoParams GenerateSymmetricCryptoParams(CryptoStrength strength)
        {
            var aesKeySize = MapStrengthToAesKeySize(strength);
            var symmetricKey = SymmetricMethods.GenerateKey(aesKeySize);
            var iv = SymmetricMethods.GenerateIV();

            return new SymmetricCryptoParams(symmetricKey, iv);
        }

        /// <summary>
        /// Convert an existing symmetric key and IV into a 'SymmetricCryptoParams' instance.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static SymmetricCryptoParams MakeSymmetricCryptoParams(byte[] key, byte[] iv)
        {
            var keyParam = new KeyParameter(key);
            return new SymmetricCryptoParams(keyParam, iv);
        }

        /// <summary>
        /// Encrypt any amount of bytes using AES/CTR/NoPadding
        /// </summary>
        /// <param name="plaintext"></param>
        /// <param name="symmetricParams"></param>
        /// <returns></returns>
        public static byte[] DoSymmetricEncryption(byte[] plaintext, SymmetricCryptoParams symmetricParams)
        {
            return SymmetricMethods.Encrypt(plaintext, symmetricParams.Key, symmetricParams.IV);
        }

        /// <summary>
        /// Decrypt bytes that were previously encrypted using AES/CTR/NoPadding
        /// </summary>
        /// <param name="ciphertext"></param>
        /// <param name="symmetricParams"></param>
        /// <returns></returns>
        public static byte[] UndoSymmetricEncryption(byte[] ciphertext, SymmetricCryptoParams symmetricParams)
        {
            return SymmetricMethods.Decrypt(ciphertext, symmetricParams.Key, symmetricParams.IV);
        }

        /// <summary>
        /// Create a new 'AsymmetricCipherKeyPair' instance of the desired strength
        /// </summary>
        /// <param name="strength"></param>
        /// <returns>An 'AsymmetricCipherKeyPair' instance, which contains a private and public key</returns>
        public static WrapsAsymmetricCipherKeyPair GenerateAsymmetricKeys(CryptoStrength strength)
        {
            var rsaKeySize = MapStrengthToRsaKeySize(strength);
            var keys = AsymmetricMethods.GenerateKeys(rsaKeySize);
            return new WrapsAsymmetricCipherKeyPair(keys.Public, keys.Private);
        }

        /// <summary>
        /// Encrypt symmetric encryption information using an asymmetric public key
        /// </summary>
        /// <param name="symmetricParams"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static byte[] EncryptSymmetricInfo(SymmetricCryptoParams symmetricParams, AsymmetricKeyParameter publicKey)
        {
            var symmetricInfo = new SymmetricInfoDTO(symmetricParams.Key.ConvertToBytes(), symmetricParams.IV);
            var symmetricInfoString = symmetricInfo.ToString();
            var symmetricInfoBytes = Encoding.UTF8.GetBytes(symmetricInfoString);

            return AsymmetricMethods.Encrypt(symmetricInfoBytes, publicKey);
        }

        /// <summary>
        /// Attempt to decrypt and deserialize a signature
        /// </summary>
        /// <param name="ciphertext"></param>
        /// <param name="pemKey"></param>
        /// <exception cref="FormatException"></exception>
        /// <returns></returns>
        public static SymmetricInfoDTO DecryptAndDeserializeSymmetricInfo(byte[] ciphertext, string pemKey)
        {
            // Get the private key from the PEM string
            var privateKey = ConvertRsaPrivateKeyFromPEM(pemKey).Private;

            // Attempt to decrypt the symmetric info
            byte[] decryptedBytes = AsymmetricMethods.Decrypt(ciphertext, privateKey);
            string decryptedString = Encoding.UTF8.GetString(decryptedBytes);

            // Attempt to deserialize the plaintext signature
            return new SymmetricInfoDTO(decryptedString);
        }

        public static AsymmetricCipherKeyPair ConvertRsaPrivateKeyFromPEM(string pemKey)
        {
            var stringReader = new StringReader(pemKey);
            var pemReader = new PemReader(stringReader);
            return (AsymmetricCipherKeyPair)pemReader.ReadObject();
        }

        public static AsymmetricKeyParameter ConvertRsaPublicKeyFromPEM(string pemKey)
        {
            var stringReader = new StringReader(pemKey);
            var pemReader = new PemReader(stringReader);
            return (AsymmetricKeyParameter)pemReader.ReadObject();
        }

        /// <summary>
        /// Return an object containing all the private key properties
        /// </summary>
        /// <param name="pemKey"></param>
        /// <returns></returns>
        public static RsaPrivateCrtKeyParameters SomethingToPlayWithLater(string pemKey)
        {
            var stringReader = new StringReader(pemKey);
            var pemReader = new PemReader(stringReader);
            return (RsaPrivateCrtKeyParameters)pemReader.ReadObject();
        }

        /// <summary>
        /// Return an object containing just the private key properties
        /// </summary>
        /// <param name="pemKey"></param>
        /// <returns></returns>
        public static RsaKeyParameters SomethingElseToPlayWithLater(string pemKey)
        {
            var stringReader = new StringReader(pemKey);
            var pemReader = new PemReader(stringReader);
            return (RsaKeyParameters)pemReader.ReadObject();
        }

        public static bool VerifyPlaintextAgainstKnownDigest(byte[] plaintext, byte[] knownDigest, DigestAlgorithm algorithm)
        {
            var newDigest = GetDigest(plaintext, algorithm);

            if (newDigest.Length != knownDigest.Length)
            {
                return false;
            }

            for (int i = 0; i < knownDigest.Length; i++)
            {
                if (!knownDigest[i].Equals(newDigest[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static byte[] GetDigest(byte[] data, DigestAlgorithm algorithm)
        {
            return DigestMethods.GetDigest(data, algorithm);
        }

        /// <summary>
        /// Digest a user's password using their username as a salt.
        /// This value is sent to the API during registration and login.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>SHA512(password:username)</returns>
        public static byte[] DigestUsernameAndPasswordForAuthentication(string username, string password)
        {
            var saltedPassword = $"{password}:{username.ToLower()}";
            var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
            return GetDigest(saltedPasswordBytes, DigestAlgorithm.SHA512);
        }

        /// <summary>
        /// Create a symmetric key to encrypt/decrypt a user's private key
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static SymmetricCryptoParams CreateSymmetricKeyFromUserDetails(string username, string password, string userId)
        {
            var keySeed = $"{username.ToLower()}:{password}";
            var seedBytes = Encoding.UTF8.GetBytes(keySeed);

            var key = GetDigest(seedBytes, DigestAlgorithm.SHA256);
            var iv = GetDigest(Encoding.UTF8.GetBytes(userId.ToLower()), DigestAlgorithm.SHA256)[0..15];
            return MakeSymmetricCryptoParams(key, iv);
        }

        public static byte[] SignPlaintext(byte[] plaintext, AsymmetricKeyParameter privateKey)
        {
            return AsymmetricMethods.DigestAndSign(plaintext, privateKey);
        }

        public static bool VerifySignature(byte[] plaintext, byte[] signature, string publicPemKey)
        {
            // Get the public key from the PEM string
            var publicKey = ConvertRsaPublicKeyFromPEM(publicPemKey);

            return AsymmetricMethods.VerifySignature(plaintext, signature, publicKey);
        }

        private static AesKeySize MapStrengthToAesKeySize(CryptoStrength strength)
        {
            return strength switch
            {
                CryptoStrength.Insecure => AesKeySize.AES128,
                CryptoStrength.Minimum => AesKeySize.AES256,
                CryptoStrength.Standard => AesKeySize.AES256,
                CryptoStrength.Maximum => AesKeySize.AES256,
                _ => throw new NotImplementedException()
            };
        }

        private static RsaKeySize MapStrengthToRsaKeySize(CryptoStrength strength)
        {
            return strength switch
            {
                CryptoStrength.Insecure => RsaKeySize._1024,
                CryptoStrength.Minimum => RsaKeySize._1024,
                CryptoStrength.Standard => RsaKeySize._2048,
                CryptoStrength.Maximum => RsaKeySize._4096,
                _ => throw new NotImplementedException()
            };
        }
    }
}
