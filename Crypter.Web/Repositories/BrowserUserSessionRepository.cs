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

using Crypter.Common.Client.DeviceStorage.Enums;
using Crypter.Common.Client.DeviceStorage.Models;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Monads;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Web.Repositories
{
   public class BrowserUserSessionRepository : IUserSessionRepository
   {
      private readonly IDeviceRepository<BrowserStorageLocation> _browserRepository;
      private readonly IReadOnlyDictionary<bool, BrowserStorageLocation> _trustDeviceStorageMap;

      public BrowserUserSessionRepository(IDeviceRepository<BrowserStorageLocation> browserRepository)
      {
         _browserRepository = browserRepository;
         _trustDeviceStorageMap = new Dictionary<bool, BrowserStorageLocation>
         {
            { false, BrowserStorageLocation.SessionStorage },
            { true, BrowserStorageLocation.LocalStorage }
         };
      }

      public Task<Maybe<UserSession>> GetUserSessionAsync()
      {
         return _browserRepository.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession);
      }

      public async Task StoreUserSessionAsync(UserSession userSession, bool trustDevice)
      {
         await _browserRepository.SetItemAsync(DeviceStorageObjectType.UserSession, userSession, _trustDeviceStorageMap[trustDevice]);
      }
   }
}
