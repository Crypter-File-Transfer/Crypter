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

using System.Text.RegularExpressions;

namespace Crypter.Common.Services
{
   public class ValidationService
   {
      public static bool IsValidPassword(string password)
      {
         return !string.IsNullOrWhiteSpace(password);
      }

      public static bool IsPossibleEmailAddress(string email)
      {
         return !string.IsNullOrEmpty(email);
      }

      public static bool IsValidEmailAddress(string email)
      {
         if (email is null)
         {
            return false;
         }

         if (email.Trim().EndsWith("."))
         {
            return false;
         }

         try
         {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
         }
         catch
         {
            return false;
         }
      }

      public static bool IsValidUsername(string username)
      {
         return UsernameMeetsLengthRequirements(username)
            && UsernameMeetsCharacterRequirements(username);
      }

      public static bool UsernameMeetsCharacterRequirements(string username)
      {
         var regex = new Regex(@"^[a-zA-Z0-9_\-]+$");
         return regex.IsMatch(username);
      }

      public static bool UsernameMeetsLengthRequirements(string username)
      {
         return !string.IsNullOrEmpty(username)
            && username.Length <= 32;
      }
   }
}
