using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.IO;

namespace Crypter.CryptoLib
{
   public static class KeyConversion
   {
      public static string ConvertToPEM(this AsymmetricKeyParameter keyParams)
      {
         var stringWriter = new StringWriter();
         var pemWriter = new PemWriter(stringWriter);
         pemWriter.WriteObject(keyParams);
         pemWriter.Writer.Flush();
         return stringWriter.ToString();
      }

      public static byte[] ConvertToBytes(this KeyParameter symmetricKey)
      {
         return symmetricKey.GetKey();
      }

      public static AsymmetricCipherKeyPair ConvertRSAPrivateKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (AsymmetricCipherKeyPair)pemReader.ReadObject();
      }

      public static AsymmetricKeyParameter ConvertRSAPublicKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (AsymmetricKeyParameter)pemReader.ReadObject();
      }

      public static X25519PrivateKeyParameters ConvertX25519PrivateKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (X25519PrivateKeyParameters)pemReader.ReadObject();
      }

      public static X25519PublicKeyParameters ConvertX25519PublicKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (X25519PublicKeyParameters)pemReader.ReadObject();
      }

      public static Ed25519PrivateKeyParameters ConvertEd25519PrivateKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (Ed25519PrivateKeyParameters)pemReader.ReadObject();
      }

      public static Ed25519PublicKeyParameters ConvertEd25519PublicKeyFromPEM(string pemKey)
      {
         var stringReader = new StringReader(pemKey);
         var pemReader = new PemReader(stringReader);
         return (Ed25519PublicKeyParameters)pemReader.ReadObject();
      }
   }
}
