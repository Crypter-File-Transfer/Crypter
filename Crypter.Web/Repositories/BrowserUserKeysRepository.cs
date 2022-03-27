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

using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.Interfaces;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Web.Repositories
{
   public class BrowserUserKeysRepository : IUserKeysRepository
   {
      private readonly IDeviceRepository<BrowserStorageLocation> _browserRepository;

      private readonly IReadOnlyDictionary<bool, BrowserStorageLocation> _trustDeviceStorageMap;

      public BrowserUserKeysRepository(IDeviceRepository<BrowserStorageLocation> browserRepository)
      {
         _browserRepository = browserRepository;

         _trustDeviceStorageMap = new Dictionary<bool, BrowserStorageLocation>
         {
            { false, BrowserStorageLocation.SessionStorage },
            { true, BrowserStorageLocation.LocalStorage }
         };
      }

      public async Task<Maybe<PEMString>> GetEd25519PrivateKeyAsync()
      {
         var storageKey = await _browserRepository.GetItemAsync<string>(DeviceStorageObjectType.Ed25519PrivateKey);
         return storageKey.Bind(x =>
         {
            return PEMString.TryFrom(x, out var key)
               ? key
               : Maybe<PEMString>.None;
         });
      }

      public async Task<Maybe<PEMString>> GetX25519PrivateKeyAsync()
      {
         var storageKey = await _browserRepository.GetItemAsync<string>(DeviceStorageObjectType.X25519PrivateKey);
         return storageKey.Bind(x =>
         {
            return PEMString.TryFrom(x, out var key)
               ? key
               : Maybe<PEMString>.None;
         });
      }

      public async Task StoreEd25519PrivateKeyAsync(PEMString privateKey, bool trustDevice)
      {
         await _browserRepository.SetItemAsync(DeviceStorageObjectType.Ed25519PrivateKey, privateKey, _trustDeviceStorageMap[trustDevice]);
      }

      public async Task StoreX25519PrivateKeyAsync(PEMString privateKey, bool trustDevice)
      {
         await _browserRepository.SetItemAsync(DeviceStorageObjectType.X25519PrivateKey, privateKey, _trustDeviceStorageMap[trustDevice]);
      }
   }
}
