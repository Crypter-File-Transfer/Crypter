using System;
using System.Text;
using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
using Crypter.CryptoLib.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Crypter.CryptoLib
{
   public static class Common
   {
      public static SymmetricCryptoParams GenerateSymmetricCryptoInfo(CryptoStrength strength)
      {
         var symmetricWrapper = new SymmetricWrapper();
         var aesBlockSize = MapStrengthToAesBlockSize(strength);
         var symmetricKey = symmetricWrapper.GenerateSymmetricKey(aesBlockSize);
         var iv = symmetricWrapper.GenerateIV();

         return new SymmetricCryptoParams(symmetricKey, iv);
      }

      public static SymmetricCryptoParams MakeSymmetricCryptoParams(byte[] key, byte[] iv)
      {
         var keyParam = new KeyParameter(key);
         return new SymmetricCryptoParams(keyParam, iv);
      }

      public static byte[] DoSymmetricEncryption(byte[] plaintext, SymmetricCryptoParams symmetricParams)
      {
         var symmetricWrapper = new SymmetricWrapper();
         return symmetricWrapper.EncryptBytes(plaintext, symmetricParams.Key, symmetricParams.IV);
      }

      public static byte[] UndoSymmetricEncryption(byte[] ciphertext, SymmetricCryptoParams symmetricParams)
      {
         var symmetricWrapper = new SymmetricWrapper();
         return symmetricWrapper.DecryptBytes(ciphertext, symmetricParams.Key, symmetricParams.IV);
      }

      public static AsymmetricCipherKeyPair GenerateAsymmetricKeys(CryptoStrength strength)
      {
         var asymmetricWrapper = new AsymmetricWrapper();
         var rsaKeySize = MapStrengthToRsaKeySize(strength);
         return asymmetricWrapper.GenerateAsymmetricKeyPair(rsaKeySize);
      }

      public static byte[] CreateEncryptedSignature(byte[] plaintext, SymmetricCryptoParams symmetricParams, AsymmetricKeyParameter publicKey, CryptoStrength strength)
      {
         var digestAlgorithm = MapStrengthToDigestAlgorithm(strength);
         byte[] messageHash = DigestWrapper.GetDigest(plaintext, digestAlgorithm);
         var encodedHash = Convert.ToBase64String(messageHash);
         var encodedSymmetricKey = Convert.ToBase64String(symmetricParams.Key.ConvertToBytes());
         var encodedIV = Convert.ToBase64String(symmetricParams.IV);
         var signature = new AnonymousSignature(digestAlgorithm, encodedHash, encodedSymmetricKey, encodedIV);
         var signatureString = signature.ToString();
         var signatureBytes = Encoding.UTF8.GetBytes(signatureString);

         var asymmetricWrapper = new AsymmetricWrapper();
         return asymmetricWrapper.Encrypt(signatureBytes, publicKey);
      }

      public static DigestAlgorithm MapStrengthToDigestAlgorithm(CryptoStrength strength)
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

      public static AesKeySize MapStrengthToAesBlockSize(CryptoStrength strength)
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

      public static RsaKeySize MapStrengthToRsaKeySize(CryptoStrength strength)
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
