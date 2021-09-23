using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.Crypto
{
   /// <summary>
   /// Elliptical curve, digital signature algorithm
   /// </summary>
   /// <remarks>
   /// https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/crypto/test/Ed25519Test.cs
   /// </remarks>
   public class ECDSA
   {
      private ISigner Signer;
      private ISigner Verifier;

      public static AsymmetricCipherKeyPair GenerateKeys()
      {
         SecureRandom random = new SecureRandom();
         Ed25519KeyPairGenerator kpg = new Ed25519KeyPairGenerator();
         kpg.Init(new Ed25519KeyGenerationParameters(random));
         return kpg.GenerateKeyPair();
      }

      public void InitializeSigner(AsymmetricKeyParameter privateKey)
      {
         Signer = new Ed25519Signer();
         Signer.Init(true, privateKey);
      }

      public void InitializeVerifier(AsymmetricKeyParameter publicKey)
      {
         Verifier = new Ed25519Signer();
         Verifier.Init(false, publicKey);
      }

      public void SignerDigestChunk(byte[] data)
      {
         Signer.BlockUpdate(data, 0, data.Length);
      }

      public void VerifierDigestChunk(byte[] data)
      {
         Verifier.BlockUpdate(data, 0, data.Length);
      }

      public byte[] GenerateSignature()
      {
         var signature = Signer.GenerateSignature();
         Signer.Reset();
         return signature;
      }

      public bool VerifySignature(byte[] signature)
      {
         var verified = Verifier.VerifySignature(signature);
         Verifier.Reset();
         return verified;
      }
   }
}
