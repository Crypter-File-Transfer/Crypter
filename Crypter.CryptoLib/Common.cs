using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
using Crypter.CryptoLib.Models;
using System;
using System.Text;
using System.Text.Json;

namespace Crypter.CryptoLib
{
   public static class Common
   {
      public static ClientEncryptResult DoAnonymousClientEncryption(byte[] plainBytes, CryptoStrength strength)
      {
         // Encipher the plaintext
         var symmetricWrapper = new SymmetricWrapper();
         var aesBlockSize = MapStrengthToAesBlockSize(strength);
         var symmetricKey = symmetricWrapper.GenerateSymmetricKey(aesBlockSize);
         var iv = symmetricWrapper.GenerateIV();
         var cipherText = symmetricWrapper.EncryptBytes(plainBytes, symmetricKey, iv);

         // Create a signature, which contains a hash of the plaintext and the plaintext public key
         var digestAlgorithm = MapStrengthToDigestAlgorithm(strength);
         byte[] messageHash = DigestWrapper.GetDigest(plainBytes, digestAlgorithm);
         var encodedHash = Convert.ToBase64String(messageHash);
         var encodedSymmetricKey = Convert.ToBase64String(symmetricKey.ConvertToBytes());
         var encodedIV = Convert.ToBase64String(iv);
         var signature = new AnonymousSignature(digestAlgorithm, encodedHash, encodedSymmetricKey, encodedIV);
         var signatureString = signature.ToString();
         var signatureBytes = Encoding.UTF8.GetBytes(signatureString);

         Console.WriteLine($"Signature length: {signatureBytes.Length}");

         // Encipher the signature using an RSA public key
         var asymmetricWrapper = new AsymmetricWrapper();
         var rsaKeySize = MapStrengthToRsaKeySize(strength);
         var asymmetricKeyPair = asymmetricWrapper.GenerateAsymmetricKeyPair(rsaKeySize);
         var encryptedSignature = asymmetricWrapper.Encrypt(signatureBytes, asymmetricKeyPair.Public);

         // Hash the symmetric key to create a server-side encryption key
         var hashedSymmetricKey = DigestWrapper.GetDigest(symmetricKey.ConvertToBytes(), DigestAlgorithm.SHA256);

         // Base64 encode everything that needs to be transmitted or displayed
         var encodedCipherText = Convert.ToBase64String(cipherText);
         var encodedSignature = Convert.ToBase64String(encryptedSignature);
         var encodedServerEncryptionKey = Convert.ToBase64String(hashedSymmetricKey);

         return new ClientEncryptResult(encodedCipherText, encodedSignature, encodedServerEncryptionKey, asymmetricKeyPair);
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
