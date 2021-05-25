using Org.BouncyCastle.Crypto;

namespace Crypter.CryptoLib.Models
{
    public class WrapsAsymmetricCipherKeyPair : AsymmetricCipherKeyPair
    {
        public WrapsAsymmetricCipherKeyPair(AsymmetricKeyParameter publicParameter, AsymmetricKeyParameter privateParameter) : base(publicParameter, privateParameter)
        {
        }
    }
}
