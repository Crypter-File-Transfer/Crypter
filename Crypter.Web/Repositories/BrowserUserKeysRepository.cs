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

using System.Collections.Generic;
using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Repositories;
using EasyMonads;

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

      public Task<Maybe<byte[]>> GetMasterKeyAsync()
      {
         return _browserRepository.GetItemAsync<byte[]>(DeviceStorageObjectType.MasterKey);
      }

      public Task<Maybe<byte[]>> GetPrivateKeyAsync()
      {
         return _browserRepository.GetItemAsync<byte[]>(DeviceStorageObjectType.PrivateKey);
      }

      public Task<Unit> StoreMasterKeyAsync(byte[] masterKey, bool trustDevice)
      {
         return _browserRepository.SetItemAsync(DeviceStorageObjectType.MasterKey, masterKey, _trustDeviceStorageMap[trustDevice]);
      }

      public Task<Unit> StorePrivateKeyAsync(byte[] privateKey, bool trustDevice)
      {
         return _browserRepository.SetItemAsync(DeviceStorageObjectType.PrivateKey, privateKey, _trustDeviceStorageMap[trustDevice]);
      }
   }
}
