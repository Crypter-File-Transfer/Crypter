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

using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Models;
using Crypter.Common.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyMonads;

namespace Crypter.Web.Repositories
{
   public class BrowserTokenRepository : ITokenRepository
   {
      private readonly IDeviceRepository<BrowserStorageLocation> _browserRepository;
      private readonly IReadOnlyDictionary<TokenType, BrowserStorageLocation> _tokenStorageMap;

      public BrowserTokenRepository(IDeviceRepository<BrowserStorageLocation> browserRepository)
      {
         _browserRepository = browserRepository;
         _tokenStorageMap = new Dictionary<TokenType, BrowserStorageLocation>
         {
            { TokenType.Authentication, BrowserStorageLocation.Memory },
            { TokenType.Session, BrowserStorageLocation.SessionStorage },
            { TokenType.Device, BrowserStorageLocation.LocalStorage }
         };
      }

      public async Task<Unit> StoreAuthenticationTokenAsync(string token)
      {
         TokenObject tokenObject = new TokenObject(TokenType.Authentication, token);
         await _browserRepository.SetItemAsync(DeviceStorageObjectType.AuthenticationToken, tokenObject, _tokenStorageMap[TokenType.Authentication]);
         return Unit.Default;
      }

      public Task<Maybe<TokenObject>> GetAuthenticationTokenAsync()
      {
         return _browserRepository.GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken);
      }

      public async Task<Unit> StoreRefreshTokenAsync(string token, TokenType tokenType)
      {
         TokenObject tokenObject = new TokenObject(tokenType, token);
         await _browserRepository.SetItemAsync(DeviceStorageObjectType.RefreshToken, tokenObject, _tokenStorageMap[tokenType]);
         return Unit.Default;
      }

      public Task<Maybe<TokenObject>> GetRefreshTokenAsync()
      {
         return _browserRepository.GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken);
      }
   }
}
