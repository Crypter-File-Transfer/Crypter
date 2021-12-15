/*
 * Copyright (C) 2021 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using System;

namespace Crypter.CryptoLib.Crypto
{
   public class SHA
   {
      private readonly IDigest Digestor;

      public SHA(SHAFunction function)
      {
         Digestor = function switch
         {
            SHAFunction.SHA1 => new Sha1Digest(),
            SHAFunction.SHA224 => new Sha224Digest(),
            SHAFunction.SHA256 => new Sha256Digest(),
            SHAFunction.SHA512 => new Sha512Digest(),
            _ => throw new NotImplementedException()
         };
      }

      public void BlockUpdate(byte[] data)
      {
         Digestor.BlockUpdate(data, 0, data.Length);
      }

      public byte[] GetDigest()
      {
         byte[] hash = new byte[Digestor.GetDigestSize()];
         Digestor.DoFinal(hash, 0);
         Digestor.Reset();
         return hash;
      }
   }
}
