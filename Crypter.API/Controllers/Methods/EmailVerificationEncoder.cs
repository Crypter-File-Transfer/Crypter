using Microsoft.IdentityModel.Tokens;
using System;

namespace Crypter.API.Controllers.Methods
{
   public class EmailVerificationEncoder
   {
      public static string EncodeVerificationCodeUrlSafe(Guid verificationCode)
      {
         return Base64UrlEncoder.Encode(
            verificationCode.ToByteArray());
      }

      public static Guid DecodeVerificationCodeFromUrlSafe(string verificationCode)
      {
         return new Guid(
            Base64UrlEncoder.DecodeBytes(verificationCode));
      }

      public static string EncodeSignatureUrlSafe(byte[] signature)
      {
         return Base64UrlEncoder.Encode(signature);
      }

      public static byte[] DecodeSignatureFromUrlSafe(string signature)
      {
         return Base64UrlEncoder.DecodeBytes(signature);
      }
   }
}
