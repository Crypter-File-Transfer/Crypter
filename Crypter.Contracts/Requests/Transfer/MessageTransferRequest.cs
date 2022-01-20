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

using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class MessageTransferRequest : ITransferRequest
   {
      public string Subject { get; set; }
      public string CipherTextBase64 { get; set; }
      public string SignatureBase64 { get; set; }
      public string ClientEncryptionIVBase64 { get; set; }
      public string ServerEncryptionKeyBase64 { get; set; }
      public string X25519PublicKeyBase64 { get; set; }
      public string Ed25519PublicKeyBase64 { get; set; }
      public DateTime RequestedExpiration { get; set; }

        [JsonConstructor]
      public MessageTransferRequest(string subject, string cipherTextBase64, string signatureBase64, string clientEncryptionIVBase64, string serverEncryptionKeyBase64, string x25519PublicKeyBase64, string ed25519PublicKeyBase64, DateTime requestedExpiration)
      {
         Subject = subject;
         CipherTextBase64 = cipherTextBase64;
         SignatureBase64 = signatureBase64;
         ClientEncryptionIVBase64 = clientEncryptionIVBase64;
         ServerEncryptionKeyBase64 = serverEncryptionKeyBase64;
         X25519PublicKeyBase64 = x25519PublicKeyBase64;
         Ed25519PublicKeyBase64 = ed25519PublicKeyBase64;
         RequestedExpiration = requestedExpiration;
      }
   }
}
