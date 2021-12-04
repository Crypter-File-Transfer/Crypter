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

using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserX25519KeyPairService : IUserPublicKeyPairService<UserX25519KeyPair>
   {
      private readonly DataContext Context;

      public UserX25519KeyPairService(DataContext context)
      {
         Context = context;
      }

      public async Task<IUserPublicKeyPair> GetUserPublicKeyPairAsync(Guid userId)
      {
         return await Context.UserX25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
      }

      public async Task<string> GetUserPublicKeyAsync(Guid userId)
      {
         var keyPair = await Context.UserX25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
         return keyPair?.PublicKey;
      }

      public async Task<bool> InsertUserPublicKeyPairAsync(Guid userId, string privateKey, string publicKey)
      {
         if (await GetUserPublicKeyPairAsync(userId) != default(UserX25519KeyPair))
         {
            return false;
         }

         var key = new UserX25519KeyPair(
             Guid.NewGuid(),
             userId,
             privateKey,
             publicKey,
             DateTime.UtcNow);

         Context.UserX25519KeyPair.Add(key);
         await Context.SaveChangesAsync();
         return true;
      }
   }
}
