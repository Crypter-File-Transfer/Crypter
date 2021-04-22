using Org.BouncyCastle.Crypto;

namespace Crypter.CryptoLib.Models
{
   public class ClientEncryptResult
   {
      /// <summary>
      /// Base64 encoded ciphertext
      /// </summary>
      public string CipherText { get; }

      /// <summary>
      /// Base64 encoded signature
      /// </summary>
      /// <remarks>
      /// This is an 'AnonymousSignature' struct that has been serialized to JSON, converted to byte[], then Base64 encoded
      /// </remarks>
      public string Signature { get; }

      /// <summary>
      /// Base64 encoded server encryption key
      /// </summary>
      /// <remarks>
      /// This is the sha256 hash of symmetric key used to create the ciphertext
      /// </remarks>
      public string ServerEncryptionKey { get; }
      public AsymmetricCipherKeyPair KeyPair { get; }

      public ClientEncryptResult(string encodedCipherText, string signature, string encodedServerEncryptionKey, AsymmetricCipherKeyPair keyPair)
      {
         CipherText = encodedCipherText;
         Signature = signature;
         ServerEncryptionKey = encodedServerEncryptionKey;
         KeyPair = keyPair;
      }
   }
}
