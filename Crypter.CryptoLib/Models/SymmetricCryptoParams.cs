using Org.BouncyCastle.Crypto.Parameters;

namespace Crypter.CryptoLib.Models
{
   public class SymmetricCryptoParams
   {
      public KeyParameter Key { get; }
      public byte[] IV { get; }

      public SymmetricCryptoParams(KeyParameter key, byte[] iv)
      {
         Key = key;
         IV = iv;
      }
   }
}
