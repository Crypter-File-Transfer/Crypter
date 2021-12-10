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

using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public enum StorageLocation
   {
      InMemory,
      SessionStorage,
      LocalStorage
   }

   public enum StoredObjectType
   {
      UserSession,
      AuthToken,
      PlaintextX25519PrivateKey,
      PlaintextEd25519PrivateKey,
      EncryptedX25519PrivateKey,
      EncryptedEd25519PrivateKey
   }

   public interface ILocalStorageService
   {
      bool IsInitialized { get; }
      Task InitializeAsync();
      Task<T> GetItemAsync<T>(StoredObjectType itemType);
      bool HasItem(StoredObjectType itemType);
      Task SetItemAsync<T>(StoredObjectType itemType, T value, StorageLocation location);
      Task RemoveItemAsync(StoredObjectType itemType);
      Task DisposeAsync();
   }

   public class LocalStorageService : ILocalStorageService
   {
      private const string SessionStorageLiteral = "sessionStorage";
      private const string LocalStorageLiteral = "localStorage";

      private readonly IJSRuntime JSRuntime;

      private readonly Dictionary<string, object> InMemoryStorage;
      private readonly Dictionary<string, StorageLocation> ObjectLocations;

      public bool IsInitialized { get; private set; } = false;

      public LocalStorageService(IJSRuntime jSRuntime)
      {
         JSRuntime = jSRuntime;
         InMemoryStorage = new Dictionary<string, object>();
         ObjectLocations = new Dictionary<string, StorageLocation>();
      }

      public async Task InitializeAsync()
      {
         if (!IsInitialized)
         {
            foreach (StoredObjectType item in Enum.GetValues(typeof(StoredObjectType)))
            {
               await InitializeItemFromLocalStorage(item);
               await InitializeItemFromSessionStorage(item);
            }
            IsInitialized = true;
         }
      }

      private async Task InitializeItemFromLocalStorage(StoredObjectType itemType)
      {
         var value = await JSRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", itemType.ToString());
         if (!string.IsNullOrEmpty(value))
         {
            ObjectLocations.Add(itemType.ToString(), StorageLocation.LocalStorage);
         }
      }

      private async Task InitializeItemFromSessionStorage(StoredObjectType itemType)
      {
         var value = await JSRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", itemType.ToString());
         if (!string.IsNullOrEmpty(value))
         {
            ObjectLocations.Add(itemType.ToString(), StorageLocation.SessionStorage);
         }
      }

      public async Task<T> GetItemAsync<T>(StoredObjectType itemType)
      {
         if (ObjectLocations.TryGetValue(itemType.ToString(), out var location))
         {
            string storedJson;
            switch (location)
            {
               case StorageLocation.InMemory:
                  return (T)InMemoryStorage[itemType.ToString()];
               case StorageLocation.SessionStorage:
                  storedJson = await JSRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", itemType.ToString());
                  break;
               case StorageLocation.LocalStorage:
                  storedJson = await JSRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", itemType.ToString());
                  break;
               default:
                  throw new NotImplementedException();
            }
            return JsonSerializer.Deserialize<T>(storedJson);
         }
         return default;
      }

      public bool HasItem(StoredObjectType itemType)
      {
         return ObjectLocations.ContainsKey(itemType.ToString());
      }

      public async Task SetItemAsync<T>(StoredObjectType itemType, T value, StorageLocation location)
      {
         ObjectLocations.TryAdd(itemType.ToString(), location);
         switch (location)
         {
            case StorageLocation.InMemory:
               InMemoryStorage.Add(itemType.ToString(), value);
               break;
            case StorageLocation.SessionStorage:
               await JSRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.setItem", itemType.ToString(), JsonSerializer.Serialize(value));
               break;
            case StorageLocation.LocalStorage:
               await JSRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.setItem", itemType.ToString(), JsonSerializer.Serialize(value));
               break;
            default:
               throw new NotImplementedException();
         }
      }

      public async Task RemoveItemAsync(StoredObjectType itemType)
      {
         if (ObjectLocations.TryGetValue(itemType.ToString(), out var location))
         {
            ObjectLocations.Remove(itemType.ToString());
            switch (location)
            {
               case StorageLocation.InMemory:
                  InMemoryStorage.Remove(itemType.ToString());
                  break;
               case StorageLocation.SessionStorage:
                  await JSRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.removeItem", itemType.ToString());
                  break;
               case StorageLocation.LocalStorage:
                  await JSRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.removeItem", itemType.ToString());
                  break;
               default:
                  throw new NotImplementedException();
            }
         }
      }

      public async Task DisposeAsync()
      {
         foreach (StoredObjectType item in Enum.GetValues(typeof(StoredObjectType)))
         {
            await RemoveItemAsync(item);
         }
      }
   }
}
