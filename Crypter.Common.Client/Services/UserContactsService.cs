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

using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.Contacts.RequestErrorCodes;
using Crypter.Common.Monads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Services
{
   public class UserContactsService : IUserContactsService, IDisposable
   {
      private readonly ICrypterApiClient _crypterApiClient;
      private readonly IUserSessionService _userSessionService;
      private IDictionary<string, UserContact> _contacts;

      private readonly SemaphoreSlim _fetchMutex = new(1);

      public UserContactsService(ICrypterApiClient crypterApiClient, IUserSessionService userSessionService)
      {
         _crypterApiClient = crypterApiClient;
         _userSessionService = userSessionService;
         _userSessionService.ServiceInitializedEventHandler += OnSessionServiceInitialized;
         _userSessionService.UserLoggedInEventHandler += OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler += OnUserLoggedOut;
      }

      public async Task InitializeAsync()
      {
         await InitializeContactsCacheAsync();
      }

      public async Task<IReadOnlyCollection<UserContact>> GetContactsAsync(bool getCached = true)
      {
         if (!getCached || _contacts is null)
         {
            await InitializeContactsCacheAsync(true);
         }

         return _contacts.Values.ToList();
      }

      public bool IsContact(string contactUsername)
      {
         return _contacts.ContainsKey(contactUsername.ToLower());
      }

      public Task<Either<AddUserContactError, UserContact>> AddContactAsync(string contactUsername)
      {
         string lowerContactUsername = contactUsername.ToLower();
         if (_contacts.ContainsKey(lowerContactUsername))
         {
            return Either<AddUserContactError, UserContact>.FromRight(_contacts[lowerContactUsername]).AsTask();
         }

         return _crypterApiClient.UserContact.AddUserContactAsync(lowerContactUsername)
            .DoRightAsync(x => _contacts.Add(lowerContactUsername, x));
      }

      public async Task RemoveContactAsync(string contactUsername)
      {
         string lowerContactUsername = contactUsername.ToLower();
         var response = await _crypterApiClient.UserContact.RemoveUserContactAsync(lowerContactUsername);
         response.IfSome(_ => _contacts.Remove(lowerContactUsername));
      }

      private Task<Dictionary<string, UserContact>> FetchContactsAsync()
      {
         return _crypterApiClient.UserContact.GetUserContactsAsync()
            .MapAsync(x => x.ToDictionary(y => y.Username.ToLower()))
            .SomeOrDefaultAsync(new Dictionary<string, UserContact>());
      }

      private async Task InitializeContactsCacheAsync(bool refreshCache = false)
      {
         await _fetchMutex.WaitAsync().ConfigureAwait(false);
         try
         {
            if (_userSessionService.Session.IsSome && (_contacts is null || refreshCache))
            {
               _contacts = await FetchContactsAsync();
            }
         }
         finally
         {
            _fetchMutex.Release();
         }
      }

      private async void OnSessionServiceInitialized(object sender, UserSessionServiceInitializedEventArgs args)
      {
         if (args.IsLoggedIn)
         {
            await InitializeContactsCacheAsync();
         }
         else
         {
            _contacts = null;
         }
      }

      private async void OnUserLoggedIn(object sender, UserLoggedInEventArgs _)
      {
         await InitializeContactsCacheAsync();
      }

      private void OnUserLoggedOut(object sender, EventArgs _)
      {
         _contacts = null;
      }

      public void Dispose()
      {
         _userSessionService.ServiceInitializedEventHandler -= OnSessionServiceInitialized;
         _userSessionService.UserLoggedInEventHandler -= OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler -= OnUserLoggedOut;
         GC.SuppressFinalize(this);
      }
   }
}
