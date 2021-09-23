using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.Crypto
{
    public static class RSA
    {
        /// <summary>
        /// Generate a random asymmetric key pair of the given RSA key size
        /// </summary>
        /// <param name="rsaKeySize"></param>
        /// <returns></returns>
        public static AsymmetricCipherKeyPair GenerateKeys(RsaKeySize rsaKeySize)
        {
            var random = new SecureRandom();
            var keyGenerationParameters = new KeyGenerationParameters(random, (int)rsaKeySize);
            var generator = new RsaKeyPairGenerator();
            generator.Init(keyGenerationParameters);
            return generator.GenerateKeyPair();
        }

        /// <summary>
        /// Encrypt some bytes using RSA
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://stackoverflow.com/questions/10783081/c-sharp-bouncycastle-rsa-encryption-and-decryption
        /// </remarks>
        public static byte[] Encrypt(byte[] plaintext, AsymmetricKeyParameter publicKey)
        {
            var engine = new RsaEngine();
            engine.Init(true, publicKey);
            return engine.ProcessBlock(plaintext, 0, plaintext.Length);
        }

        /// <summary>
        /// Decrypt some bytes using RSA
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://stackoverflow.com/questions/10783081/c-sharp-bouncycastle-rsa-encryption-and-decryption
        /// </remarks>
        public static byte[] Decrypt(byte[] ciphertext, AsymmetricKeyParameter privateKey)
        {
            var engine = new RsaEngine();
            engine.Init(false, privateKey);
            return engine.ProcessBlock(ciphertext, 0, ciphertext.Length);
        }

        /// <summary>
        /// Generate a signature
        /// </summary>
        /// <param name="plaintext"></param>
        /// <param name="privateKey"></param>
        /// <remarks>https://stackoverflow.com/a/8845111</remarks>
        /// <returns></returns>
        public static byte[] DigestAndSign(byte[] plaintext, AsymmetricKeyParameter privateKey)
        {
            var signer = SignerUtilities.GetSigner("SHA256withRSA");
            signer.Init(true, privateKey);

            signer.BlockUpdate(plaintext, 0, plaintext.Length);
            var signature = signer.GenerateSignature();
            signer.Reset();
            return signature;
        }

        /// <summary>
        /// Verify a signature
        /// </summary>
        /// <param name="dataToVerify"></param>
        /// <param name="signature"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static bool VerifySignature(byte[] dataToVerify, byte[] signature, AsymmetricKeyParameter publicKey)
        {
            var signer = SignerUtilities.GetSigner("SHA256withRSA");
            signer.Init(false, publicKey);

            signer.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
            var result = signer.VerifySignature(signature);
            signer.Reset();
            return result;
        }
    }
}
