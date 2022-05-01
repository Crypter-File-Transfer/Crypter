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

using Crypter.Core.Entities;
using Crypter.Core.Entities.Interfaces;
using Crypter.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserX25519KeyPairService : IUserPublicKeyPairService<UserX25519KeyPairEntity>
   {
      private readonly DataContext Context;

      public UserX25519KeyPairService(DataContext context)
      {
         Context = context;
      }

      public async Task<IUserPublicKeyPair> GetUserPublicKeyPairAsync(Guid userId, CancellationToken cancellationToken)
      {
         return await Context.UserX25519KeyPairs
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync(cancellationToken);
      }

      public async Task<string> GetUserPublicKeyAsync(Guid userId, CancellationToken cancellationToken)
      {
         var keyPair = await Context.UserX25519KeyPairs
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync(cancellationToken);
         return keyPair?.PublicKey;
      }

      public async Task<bool> InsertUserPublicKeyPairAsync(Guid userId, string privateKey, string publicKey, string clientIV, CancellationToken cancellationToken)
      {
         if (await GetUserPublicKeyPairAsync(userId, cancellationToken) != default(UserX25519KeyPairEntity))
         {
            return false;
         }

         var key = new UserX25519KeyPairEntity(
             userId,
             privateKey,
             publicKey,
             clientIV,
             DateTime.UtcNow);

         Context.UserX25519KeyPairs.Add(key);
         await Context.SaveChangesAsync(cancellationToken);
         return true;
      }
   }
}
