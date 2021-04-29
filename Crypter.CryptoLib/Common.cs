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
      public static AsymmetricCipherKeyPair GenerateAsymmetricKeys(CryptoStrength strength)
      {
         var rsaKeySize = MapStrengthToRsaKeySize(strength);
         return AsymmetricMethods.GenerateKeys(rsaKeySize);
      }

      /// <summary>
      /// Create a new, encrypted signature.
      /// </summary>
      /// <param name="plaintext"></param>
      /// <param name="symmetricParams"></param>
      /// <param name="publicKey"></param>
      /// <param name="strength"></param>
      /// <returns></returns>
      public static byte[] CreateEncryptedSignature(byte[] plaintext, SymmetricCryptoParams symmetricParams, AsymmetricKeyParameter publicKey, CryptoStrength strength)
      {
         var digestAlgorithm = MapStrengthToDigestAlgorithm(strength);
         byte[] digest = DigestMethods.GetDigest(plaintext, digestAlgorithm);
         var signature = new AnonymousSignature(digestAlgorithm, digest, symmetricParams.Key.ConvertToBytes(), symmetricParams.IV);
         var signatureString = signature.ToString();
         var signatureBytes = Encoding.UTF8.GetBytes(signatureString);

         return AsymmetricMethods.Encrypt(signatureBytes, publicKey);
      }

      /// <summary>
      /// Attempt to decrypt and deserialize a signature
      /// </summary>
      /// <param name="ciphertext"></param>
      /// <param name="pemKey"></param>
      /// <exception cref="FormatException"></exception>
      /// <returns></returns>
      public static AnonymousSignature DecryptAndDeserializeSignature(byte[] ciphertext, string pemKey)
      {
         // Get the private key from the PEM string
         var privateKey = ConvertRsaPrivateKeyFromPEM(pemKey).Private;

         // Attempt to decrypt the signature
         byte[] decryptedSignatureBytes = AsymmetricMethods.Decrypt(ciphertext, privateKey);
         string decryptedSignatureString = Encoding.UTF8.GetString(decryptedSignatureBytes);

         // Attempt to deserialize the plaintext signature
         return new AnonymousSignature(decryptedSignatureString);
      }

      public static AsymmetricCipherKeyPair ConvertRsaPrivateKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (AsymmetricCipherKeyPair)pemReader.ReadObject();
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

      public static bool VerifyPlaintextAgainstKnownDigest(byte[] plaintext, byte[] knownDigest)
      {
         var newDigest = DigestMethods.GetDigest(plaintext, DigestAlgorithm.SHA256);

         if (knownDigest.Length < (256 / 8))
         {
            return false;
         }

         for (int i = 0; i < (256 / 8); i++)
         {
            if (!knownDigest[i].Equals(newDigest[i]))
            {
               return false;
            }
         }
         return true;
      }

      private static DigestAlgorithm MapStrengthToDigestAlgorithm(CryptoStrength strength)
      {
         return strength switch
         {
            CryptoStrength.Insecure => DigestAlgorithm.SHA1,
            CryptoStrength.Minimum => DigestAlgorithm.SHA256,
            CryptoStrength.Standard => DigestAlgorithm.SHA256,
            CryptoStrength.Maximum => DigestAlgorithm.SHA256,
            _ => throw new NotImplementedException()
         };
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
