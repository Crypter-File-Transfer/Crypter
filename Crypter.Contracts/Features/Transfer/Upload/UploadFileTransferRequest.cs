/*
 * Copyright (C) 2022 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.Common.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Crypter.Contracts.Features.Transfer
{
   public class UploadFileTransferRequest : IUploadTransferRequest
   {
      public string Filename { get; init; }
      public string ContentType { get; init; }
      public string InitializationVector { get; init; }
      public List<string> Ciphertext { get; init; }
      public string DigitalSignature { get; init; }
      public string RecipientProof { get; init; }
      public string DiffieHellmanPublicKey { get; init; }
      public string DigitalSignaturePublicKey { get; init; }
      public int LifetimeHours { get; init; }
      public CompressionType CompressionType { get; init; }

      [JsonConstructor]
      public UploadFileTransferRequest(string fileName, string contentType, string initializationVector, List<string> ciphertext, string digitalSignature, string digitalSignaturePublicKey, string diffieHellmanPublicKey, string recipientProof, int lifetimeHours, CompressionType compressionType)
      {
         Filename = fileName;
         ContentType = contentType;
         InitializationVector = initializationVector;
         Ciphertext = ciphertext;
         DigitalSignature = digitalSignature;
         DigitalSignaturePublicKey = digitalSignaturePublicKey;
         DiffieHellmanPublicKey = diffieHellmanPublicKey;
         RecipientProof = recipientProof;
         LifetimeHours = lifetimeHours;
         CompressionType = compressionType;
      }
   }
}
