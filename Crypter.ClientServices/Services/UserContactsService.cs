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
using Crypter.Contracts.Features.Contacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class UserContactsService : IUserContactsService, IDisposable
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly IUserSessionService _userSessionService;
      private IDictionary<string, UserContactDTO> _contacts;

      public UserContactsService(ICrypterApiService crypterApiService, IUserSessionService userSessionService)
      {
         _crypterApiService = crypterApiService;
         _userSessionService = userSessionService;
         _userSessionService.UserLoggedInEventHandler += OnUserSessionStateChanged;
         _userSessionService.ServiceInitializedEventHandler += OnUserSessionStateChanged;
         _userSessionService.UserLoggedOutEventHandler += OnUserSessionStateChanged;
      }

      public async Task<IReadOnlyCollection<UserContactDTO>> GetContactsAsync(bool getCached = true)
      {
         if (!getCached)
         {
            await FetchAndStoreContactsAsync();
         }

         return _contacts.Values.ToList();
      }

      public bool IsContact(string contactUsername)
      {
         return _contacts.ContainsKey(contactUsername.ToLower());
      }

      public async Task<Either<AddUserContactError, UserContactDTO>> AddContactAsync(string contactUsername)
      {
         string lowerContactUsername = contactUsername.ToLower();
         if (_contacts.ContainsKey(lowerContactUsername))
         {
            return Either<AddUserContactError, UserContactDTO>.FromRight(_contacts[lowerContactUsername]);
         }

         var request = new AddUserContactRequest(lowerContactUsername);

         return await _crypterApiService.AddUserContactAsync(request)
            .BindAsync(x =>
            {
               _contacts.Add(lowerContactUsername, x.Contact);
               return Either<AddUserContactError, UserContactDTO>.FromRight(x.Contact).AsTask();
            });
      }

      public async Task RemoveContactAsync(string contactUsername)
      {
         string lowerContactUsername = contactUsername.ToLower();
         var request = new RemoveUserContactRequest(lowerContactUsername);
         var response = await _crypterApiService.RemoveUserContactAsync(request);
         response.DoRight(x => _contacts.Remove(lowerContactUsername));
      }

      private async Task<IDictionary<string, UserContactDTO>> FetchContactsAsync()
      {
         var response = await _crypterApiService.GetUserContactsAsync();
         return response.Match(
            new Dictionary<string, UserContactDTO>(),
            right => right.Contacts.ToDictionary(x => x.Username.ToLower()));
      }

      private async Task FetchAndStoreContactsAsync()
      {
         var contacts = await FetchContactsAsync();
         _contacts = contacts;
      }

      private async void OnUserSessionStateChanged(object sender, EventArgs _)
      {
         if (_userSessionService.LoggedIn)
         {
            await FetchAndStoreContactsAsync();
         }
         else
         {
            _contacts = null;
         }
      }

      public void Dispose()
      {
         _userSessionService.UserLoggedInEventHandler -= OnUserSessionStateChanged;
         _userSessionService.ServiceInitializedEventHandler -= OnUserSessionStateChanged;
         _userSessionService.UserLoggedOutEventHandler -= OnUserSessionStateChanged;
         GC.SuppressFinalize(this);
      }
   }
}
