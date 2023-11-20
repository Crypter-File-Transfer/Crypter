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
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Requests;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.Contacts.RequestErrorCodes;
using EasyMonads;

namespace Crypter.Common.Client.HttpClients.Requests;

public class UserContactRequests : IUserContactRequests
{
    private readonly ICrypterAuthenticatedHttpClient _crypterAuthenticatedHttpClient;

    public UserContactRequests(ICrypterAuthenticatedHttpClient crypterAuthenticatedHttpClient)
    {
        _crypterAuthenticatedHttpClient = crypterAuthenticatedHttpClient;
    }

    public Task<Maybe<List<UserContact>>> GetUserContactsAsync()
    {
        string url = "api/user/contact";
        return _crypterAuthenticatedHttpClient.GetMaybeAsync<List<UserContact>>(url);
    }

    public Task<Either<AddUserContactError, UserContact>> AddUserContactAsync(string contactUsername)
    {
        string url = $"api/user/contact?username={contactUsername}";
        return _crypterAuthenticatedHttpClient.PostEitherAsync<UserContact>(url, false)
            .ExtractErrorCode<AddUserContactError, UserContact>();
    }

    public Task<Maybe<Unit>> RemoveUserContactAsync(string contactUsername)
    {
        string url = $"api/user/contact?username={contactUsername}";
        return _crypterAuthenticatedHttpClient.DeleteUnitResponseAsync(url);
    }
}
