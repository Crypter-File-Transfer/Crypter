using Crypter.CryptoLib.Enums;
using System;
using System.Text;

namespace Crypter.CryptoLib.Models
{
   public class AnonymousSignature
   {
      public DigestAlgorithm Algorithm { get; }
      public byte[] Digest { get; }
      public byte[] Key { get; }
      public byte[] IV { get; }

      /// <summary>
      /// Create a new signature from params
      /// </summary>
      /// <param name="digestAlgorithm"></param>
      /// <param name="digest"></param>
      /// <param name="symmetricKey"></param>
      /// <param name="symmetricIV"></param>
      public AnonymousSignature(DigestAlgorithm digestAlgorithm, byte[] digest, byte[] symmetricKey, byte[] symmetricIV)
      {
         Algorithm = digestAlgorithm;
         Digest = digest;
         Key = symmetricKey;
         IV = symmetricIV;
      }

      /// <summary>
      /// Create a new signature from a signature string
      /// </summary>
      /// <exception cref="FormatException"></exception>
      /// <returns></returns>
      public AnonymousSignature(string signature)
      {
         var pieces = signature.Split('\n');
         if (pieces.Length != 4)
         {
            throw new FormatException("Cannot instantiate from provided string");
         }

         Algorithm = (DigestAlgorithm)Enum.Parse(typeof(DigestAlgorithm), pieces[0], true);
         Digest = Convert.FromBase64String(pieces[1]);
         Key = Convert.FromBase64String(pieces[2]);
         IV = Convert.FromBase64String(pieces[3]);
      }

      /// <summary>
      /// Serialize an AnonymousSignature
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         return $"{Algorithm}\n{Convert.ToBase64String(Digest)}\n{Convert.ToBase64String(Key)}\n{Convert.ToBase64String(IV)}";
      }
   }
}
