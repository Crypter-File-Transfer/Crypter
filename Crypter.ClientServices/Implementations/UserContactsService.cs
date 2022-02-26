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
using Crypter.Common.Monads;
using Crypter.Contracts.Features.User.AddContact;
using Crypter.Contracts.Features.User.GetContacts;
using Crypter.Contracts.Features.User.RemoveUserContact;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Implementations
{
   public class UserContactsService : IUserContactsService
   {
      private readonly ICrypterApiService _crypterApiService;
      private IDictionary<Guid, UserContactDTO> _contacts;

      public UserContactsService(ICrypterApiService crypterApiService)
      {
         _crypterApiService = crypterApiService;
      }

      public async Task InitializeAsync()
      {
         var contacts = await FetchContactsAsync();
         _contacts = contacts;
      }

      public async Task<IReadOnlyCollection<UserContactDTO>> GetContactsAsync(bool getCached = true)
      {
         if (!getCached)
         {
            var contacts = await FetchContactsAsync();
            _contacts = contacts;
         }
         
         return _contacts.Values.ToList();
      }

      public bool IsContact(Guid userId)
      {
         return _contacts.ContainsKey(userId);
      }

      public async Task<Either<AddUserContactError, UserContactDTO>> AddContactAsync(Guid userId)
      {
         if (_contacts.ContainsKey(userId))
         {
            return _contacts[userId];
         }

         var request = new AddUserContactRequest(userId);
         var response = await _crypterApiService.AddUserContactAsync(request);
         response.DoRight(x => _contacts.Add(x.Contact.Id, x.Contact));
         return response.Match<Either<AddUserContactError, UserContactDTO>>(
            left => left,
            right => right.Contact);
      }

      public async Task RemoveContactAsync(Guid userId)
      {
         var request = new RemoveUserContactRequest(userId);
         var response = await _crypterApiService.RemoveUserContactAsync(request);
         response.DoRight(x => _contacts.Remove(userId));
      }

      public void Dispose()
      {
         _contacts = null;
      }

      private async Task<IDictionary<Guid, UserContactDTO>> FetchContactsAsync()
      {
         var response = await _crypterApiService.GetUserContactsAsync();
         return response.Match(
            left => new Dictionary<Guid, UserContactDTO>(),
            right => right.Contacts.ToDictionary(x => x.Id));
      }
   }
}
