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

using Crypter.Common.Enums;
using EasyMonads;

namespace Crypter.Common.Client.Transfer.Models
{
   public class UploadHandlerResponse
   {
      public string TransferId { get; init; }
      public int ExpirationHours { get; init; }
      public TransferItemType ItemType { get; init; }
      public TransferUserType UserType { get; init; }
      public Maybe<byte[]> RecipientKeySeed { get; init; }

      public UploadHandlerResponse(string transferId, int expirationHours, TransferItemType itemType, TransferUserType userType, Maybe<byte[]> recipientKeySeed)
      {
         TransferId = transferId;
         ExpirationHours = expirationHours;
         ItemType = itemType;
         UserType = userType;
         RecipientKeySeed = recipientKeySeed;
      }
   }
}
