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

using System.Threading.Tasks;

namespace Crypter.ClientServices.Interfaces
{
   /// <summary>
   /// This interface is intended to be used by other services that
   /// need access to the locally stored authentication and refresh tokens.
   /// 
   /// A class that implements this interface will likely need to implement
   /// some other methods that save or store these tokens on the device.
   /// </summary>
   public interface ITokenRepository
   {
      Task StoreAuthenticationTokenAsync(string token);
      Task<string> GetAuthenticationTokenAsync();
      Task StoreRefreshTokenAsync(string token);
      Task<string> GetRefreshTokenAsync();
   }
}
