using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.Crypto
{
   /// <summary>
   /// Elliptical curve, Diffie-Hellman
   /// </summary>
   /// <remarks>
   /// https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/crypto/test/X25519Test.cs
   /// </remarks>
   public class ECDH
   {
      public static AsymmetricCipherKeyPair GenerateKeys()
      {
         SecureRandom random = new SecureRandom();
         IAsymmetricCipherKeyPairGenerator kpGen = new X25519KeyPairGenerator();
         kpGen.Init(new X25519KeyGenerationParameters(random));
         return kpGen.GenerateKeyPair();
      }

      /// <summary>
      /// Derive a shared key using one party's private key and the other party's public key
      /// </summary>
      /// <param name="privateKey">Alice's private key</param>
      /// <param name="publicKey">Bob's public key</param>
      /// <returns>256-bit shared key</returns>
      /// <remarks>
      /// It is not recommended to use this shared key directly, since it is not truly random.
      /// Use 'DeriveSharedKeys()' instead; the keys are truly random.
      /// </remarks>
      public static byte[] DeriveSharedKey(AsymmetricKeyParameter privateKey, AsymmetricKeyParameter publicKey)
      {
         X25519Agreement agreement = new X25519Agreement();
         agreement.Init(privateKey);
         byte[] sharedSecret = new byte[agreement.AgreementSize];
         agreement.CalculateAgreement(publicKey, sharedSecret, 0);
         return sharedSecret;
      }

      /// <summary>
      /// Derive a pair of shared keys using one party's private key and the other party's public key
      /// </summary>
      /// <param name="keyPair">Alice's key pair</param>
      /// <param name="publicKey">Bob's public key</param>
      /// <returns>A ReceiveKey for Alice and a SendKey for Bob</returns>
      public static (byte[] ReceiveKey, byte[] SendKey) DeriveSharedKeys(AsymmetricCipherKeyPair keyPair, AsymmetricKeyParameter publicKey)
      {
         var sharedKey = DeriveSharedKey(keyPair.Private, publicKey);

         var receiveDigestor = new SHA(SHAFunction.SHA256);
         receiveDigestor.BlockUpdate(sharedKey);
         receiveDigestor.BlockUpdate(((X25519PublicKeyParameters)keyPair.Public).GetEncoded());
         receiveDigestor.BlockUpdate(((X25519PublicKeyParameters)publicKey).GetEncoded());
         var receiveKey = receiveDigestor.GetDigest();

         var sendDigestor = new SHA(SHAFunction.SHA256);
         sendDigestor.BlockUpdate(sharedKey);
         sendDigestor.BlockUpdate(((X25519PublicKeyParameters)publicKey).GetEncoded());
         sendDigestor.BlockUpdate(((X25519PublicKeyParameters)keyPair.Public).GetEncoded());
         var sendKey = sendDigestor.GetDigest();

         return (receiveKey, sendKey);
      }
   }
}
