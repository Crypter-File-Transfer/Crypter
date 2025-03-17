/*
 * Copyright (C) 2025 Crypter File Transfer
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

using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.DataContextExtensions;

internal static class DbContextExtensions
{
    /// <summary>
    /// Checks whether the provided email address is available for the given user.
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>
    /// Though null and empty email addresses would be considered available, those checks should be performed
    /// outside of this method.
    /// </remarks>
    public static async Task<bool> IsEmailAddressAvailableAsync(this DataContext dataContext, EmailAddress email, CancellationToken cancellationToken = default)
    {
        return !await dataContext.Users
            .AnyAsync(x => x.EmailAddress == email.Value || x.EmailChange!.EmailAddress == email.Value, cancellationToken);
    }
}
