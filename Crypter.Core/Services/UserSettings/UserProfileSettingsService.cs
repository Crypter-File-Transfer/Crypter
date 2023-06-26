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

using Crypter.Common.Contracts.Features.UserSettings.ProfileSettings;
using Crypter.Core.Features.UserSettings.ProfileSettings;
using System;
using System.Threading;
using System.Threading.Tasks;
using EasyMonads;

namespace Crypter.Core.Services.UserSettings
{
   public interface IUserProfileSettingsService
   {
      Task<Maybe<ProfileSettings>> GetProfileSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
      Task<Either<SetProfileSettingsError, ProfileSettings>> SetProfileSettingsAsync(Guid userId, ProfileSettings request);
   }

   public class UserProfileSettingsService : IUserProfileSettingsService
   {
      private readonly DataContext _context;

      public UserProfileSettingsService(DataContext context)
      {
         _context = context;
      }

      public async Task<Maybe<ProfileSettings>> GetProfileSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
      {
         return await UserProfileSettingsQueries.GetProfileSettingsAsync(_context, userId, cancellationToken);
      }

      public async Task<Either<SetProfileSettingsError, ProfileSettings>> SetProfileSettingsAsync(Guid userId, ProfileSettings request)
      {
         return await UserProfileSettingsCommands.SetProfileSettingsAsync(_context, userId, request);
      }
   }
}
