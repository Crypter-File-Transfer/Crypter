using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.IO;

namespace Crypter.CryptoLib.BouncyCastle
{
   public static class ExtMethods
   {
      public static string ConvertToPEM(this AsymmetricKeyParameter keyParams)
      {
         var stringWriter = new StringWriter();
         var pemWriter = new PemWriter(stringWriter);
         pemWriter.WriteObject(keyParams);
         pemWriter.Writer.Flush();
         return stringWriter.ToString();
      }

      // Dead code that I don't want to delete yet - Jack
      /*
      public static string ConvertToPKCS8(this RsaKeyParameters privateKeyParams)
      {
         var pkcs8 = new Pkcs8Generator(privateKeyParams);
         var pem = pkcs8.Generate();

         var stringWriter = new StringWriter();
         var pemWriter = new PemWriter(stringWriter);
         pemWriter.WriteObject(pem);
         pemWriter.Writer.Flush();
         return stringWriter.ToString();
      }
      */

      public static byte[] ConvertToBytes(this KeyParameter symmetricKey)
      {
         return symmetricKey.GetKey();
      }
   }
}
