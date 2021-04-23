using Crypter.CryptoLib.Enums;
using System;

namespace Crypter.CryptoLib
{
   public class AnonymousSignature
   {
      public DigestAlgorithm Algorithm { get; }
      public string Digest { get; }
      public string Key { get; }
      public string IV { get; }

      /// <summary>
      /// This is the proper way to create a new AnonymousSignature
      /// </summary>
      /// <param name="digestAlgorithm"></param>
      /// <param name="signedDigestEncodedBase64"></param>
      /// <param name="symmetricKeyEncodedBase64"></param>
      /// <param name="symmetricIVEncodedBase64"></param>
      public AnonymousSignature(DigestAlgorithm digestAlgorithm, string signedDigestEncodedBase64, string symmetricKeyEncodedBase64, string symmetricIVEncodedBase64)
      {
         Algorithm = digestAlgorithm;
         Digest = signedDigestEncodedBase64;
         Key = symmetricKeyEncodedBase64;
         IV = symmetricIVEncodedBase64;
      }

      /// <summary>
      /// Only use this to de-serialize an existing AnonymousSignature from a string
      /// </summary>
      /// <returns></returns>
      public AnonymousSignature(string existingSignature)
      {
         var pieces = existingSignature.Split('\n');
         Algorithm = (DigestAlgorithm)Enum.Parse(typeof(DigestAlgorithm), pieces[0], true);
         Digest = pieces[1];
         Key = pieces[2];
         IV = pieces[3];
      }

      /// <summary>
      /// The correct way to serialize an AnonymousSignature
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         return $"{Algorithm}\n{Digest}\n{Key}\n{IV}";
      }
   }
}
