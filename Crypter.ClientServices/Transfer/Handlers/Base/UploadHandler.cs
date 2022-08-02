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

using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.CryptoLib;

/*
namespace Crypter.ClientServices.Transfer.Handlers.Base
{
   public class UploadHandler : IUserUploadHandler
   {
      protected readonly ICrypterApiService _crypterApiService;
      protected readonly FileTransferSettings _fileTransferSettings;

      protected TransferUserType _transferUserType = TransferUserType.Anonymous;

      protected bool _senderDefined = false;

      protected Maybe<PEMString> _senderDiffieHellmanPrivateKey = Maybe<PEMString>.None;
      protected Maybe<PEMString> _senderDiffieHellmanPublicKey = Maybe<PEMString>.None;

      protected Maybe<PEMString> _senderDigitalSignaturePrivateKey = Maybe<PEMString>.None;
      protected Maybe<PEMString> _senderDigitalSignaturePublicKey = Maybe<PEMString>.None;

      protected Maybe<string> _recipientUsername = Maybe<string>.None;

      protected Maybe<PEMString> _recipientDiffieHellmanPrivateKey = Maybe<PEMString>.None;
      protected Maybe<PEMString> _recipientDiffieHellmanPublicKey = Maybe<PEMString>.None;

      public UploadHandler(ICrypterApiService crypterApiService, FileTransferSettings fileTransferSettings)
      {
         _crypterApiService = crypterApiService;
         _fileTransferSettings = fileTransferSettings;
      }

      public void SetSenderInfo(PEMString diffieHellmanPrivateKey, PEMString digitalSignaturePrivateKey)
      {
         _senderDefined = true;
         _transferUserType = TransferUserType.User;

         var senderX25519PrivateKeyDecoded = KeyConversion.ConvertX25519PrivateKeyFromPEM(diffieHellmanPrivateKey);
         var senderX25519PublicKeyDecoded = senderX25519PrivateKeyDecoded.GeneratePublicKey();

         _senderDiffieHellmanPrivateKey = diffieHellmanPrivateKey;
         _senderDiffieHellmanPublicKey = senderX25519PublicKeyDecoded.ConvertToPEM();

         var senderEd25519PrivateKeyDecoded = KeyConversion.ConvertEd25519PrivateKeyFromPEM(digitalSignaturePrivateKey);
         var senderEd25519PublicKeyDecoded = senderEd25519PrivateKeyDecoded.GeneratePublicKey();

         _senderDigitalSignaturePrivateKey = digitalSignaturePrivateKey;
         _senderDigitalSignaturePublicKey = senderEd25519PublicKeyDecoded.ConvertToPEM();
      }

      public void SetRecipientInfo(string username, PEMString diffieHellmanPublicKey)
      {
         _transferUserType = TransferUserType.User;
         _recipientUsername = username;
         _recipientDiffieHellmanPublicKey = diffieHellmanPublicKey;
      }

      protected void CreateEphemeralSenderKeys()
      {
         var senderX25519KeyPair = ECDH.GenerateKeys();
         _senderDiffieHellmanPrivateKey = senderX25519KeyPair.Private.ConvertToPEM();
         _senderDiffieHellmanPublicKey = senderX25519KeyPair.Public.ConvertToPEM();

         var senderEd25519KeyPair = ECDSA.GenerateKeys();
         _senderDigitalSignaturePrivateKey = senderEd25519KeyPair.Private.ConvertToPEM();
         _senderDigitalSignaturePublicKey = senderEd25519KeyPair.Public.ConvertToPEM();
      }

      protected void CreateEphemeralRecipientKeys()
      {
         var recipientX25519KeyPair = ECDH.GenerateKeys();
         _recipientDiffieHellmanPrivateKey = recipientX25519KeyPair.Private.ConvertToPEM();
         _recipientDiffieHellmanPublicKey = recipientX25519KeyPair.Public.ConvertToPEM();
      }

      protected static (byte[] SendKey, byte[] ServerKey) DeriveSymmetricKeys(PEMString senderX25519PrivateKey, PEMString recipientX25519PublicKey)
      {
         var senderX25519PrivateDecoded = KeyConversion.ConvertX25519PrivateKeyFromPEM(senderX25519PrivateKey);
         var senderX25519PublicDecoded = senderX25519PrivateDecoded.GeneratePublicKey();
         var senderKeyPair = new AsymmetricCipherKeyPair(senderX25519PublicDecoded, senderX25519PrivateDecoded);
         var recipientX25519PublicDecoded = KeyConversion.ConvertX25519PublicKeyFromPEM(recipientX25519PublicKey);
         (var receiveKey, var sendKey) = ECDH.DeriveSharedKeys(senderKeyPair, recipientX25519PublicDecoded);
         var digestor = new SHA(SHAFunction.SHA256);
         digestor.BlockUpdate(sendKey);
         var serverKey = ECDH.DeriveKeyFromECDHDerivedKeys(receiveKey, sendKey);

         return (sendKey, serverKey);
      }
   }
}
*/