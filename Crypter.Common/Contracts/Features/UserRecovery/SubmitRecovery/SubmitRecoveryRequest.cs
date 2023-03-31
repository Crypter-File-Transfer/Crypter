/*
 * Copyright (C) 2023 Crypter File Transfer
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

using Crypter.Common.Contracts.Features.UserAuthentication;

namespace Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery
{
   public class SubmitRecoveryRequest
   {
      public string Username { get; set; }
      public string RecoveryCode { get; set; }
      public string RecoverySignature { get; set; }
      public VersionedPassword VersionedPassword { get; set; }
      public ReplacementMasterKeyInformation ReplacementMasterKeyInformation { get; set; }

      public SubmitRecoveryRequest(string username, string recoveryCode, string recoverySignature, VersionedPassword versionedPassword, ReplacementMasterKeyInformation replacementMasterKeyInformation = null)
      {
         Username = username;
         RecoveryCode = recoveryCode;
         RecoverySignature = recoverySignature;
         VersionedPassword = versionedPassword;
         ReplacementMasterKeyInformation = replacementMasterKeyInformation;
      }
   }

   public class ReplacementMasterKeyInformation
   {
      public byte[] CurrentRecoveryProof { get; set; }
      public byte[] NewRecoveryProof { get; init; }
      public byte[] EncryptedKey { get; set; }
      public byte[] Nonce { get; init; }

      public ReplacementMasterKeyInformation(byte[] currentRecoveryProof, byte[] newRecoveryProof, byte[] encryptedKey, byte[] nonce)
      {
         CurrentRecoveryProof = currentRecoveryProof;
         NewRecoveryProof = newRecoveryProof;
         EncryptedKey = encryptedKey;
         Nonce = nonce;
      }
   }
}
