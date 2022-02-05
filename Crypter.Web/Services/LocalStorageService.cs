﻿/*
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
      AuthenticationToken,
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
      StorageLocation GetItemLocation(StoredObjectType itemType);
      Task SetItemAsync<T>(StoredObjectType itemType, T value, StorageLocation location);
      Task RemoveItemAsync(StoredObjectType itemType);
      Task DisposeAsync();
   }

   public class LocalStorageService : ILocalStorageService
   {
      public const string SessionStorageLiteral = "sessionStorage";
      public const string LocalStorageLiteral = "localStorage";

      private readonly IJSRuntime _jsRuntime;

      private readonly Dictionary<string, object> _inMemoryStorage;
      private readonly Dictionary<string, StorageLocation> _objectLocations;

      public bool IsInitialized { get; private set; } = false;

      public LocalStorageService(IJSRuntime jSRuntime)
      {
         _jsRuntime = jSRuntime;
         _inMemoryStorage = new Dictionary<string, object>();
         _objectLocations = new Dictionary<string, StorageLocation>();
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
         var value = await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", new object[] { itemType.ToString() });
         if (!string.IsNullOrEmpty(value))
         {
            _objectLocations.Add(itemType.ToString(), StorageLocation.LocalStorage);
         }
      }

      private async Task InitializeItemFromSessionStorage(StoredObjectType itemType)
      {
         var value = await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", new object[] { itemType.ToString() });
         if (!string.IsNullOrEmpty(value))
         {
            _objectLocations.Add(itemType.ToString(), StorageLocation.SessionStorage);
         }
      }

      public async Task<T> GetItemAsync<T>(StoredObjectType itemType)
      {
         if (_objectLocations.TryGetValue(itemType.ToString(), out var location))
         {
            string storedJson;
            switch (location)
            {
               case StorageLocation.InMemory:
                  return (T)_inMemoryStorage[itemType.ToString()];
               case StorageLocation.SessionStorage:
                  storedJson = await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", new object[] { itemType.ToString() });
                  break;
               case StorageLocation.LocalStorage:
                  storedJson = await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", new object[] { itemType.ToString() });
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
         return _objectLocations.ContainsKey(itemType.ToString());
      }

      public StorageLocation GetItemLocation(StoredObjectType itemType)
      {
         return _objectLocations[itemType.ToString()];
      }

      public async Task SetItemAsync<T>(StoredObjectType itemType, T value, StorageLocation location)
      {
         await RemoveItemAsync(itemType);

         switch (location)
         {
            case StorageLocation.InMemory:
               _inMemoryStorage.Add(itemType.ToString(), value);
               break;
            case StorageLocation.SessionStorage:
               await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.setItem", new object[] { itemType.ToString(), JsonSerializer.Serialize(value) });
               break;
            case StorageLocation.LocalStorage:
               await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.setItem", new object[] { itemType.ToString(), JsonSerializer.Serialize(value) });
               break;
            default:
               throw new NotImplementedException();
         }

         _objectLocations.Add(itemType.ToString(), location);
      }

      public async Task RemoveItemAsync(StoredObjectType itemType)
      {
         if (_objectLocations.TryGetValue(itemType.ToString(), out var location))
         {
            _objectLocations.Remove(itemType.ToString());
            switch (location)
            {
               case StorageLocation.InMemory:
                  _inMemoryStorage.Remove(itemType.ToString());
                  break;
               case StorageLocation.SessionStorage:
                  await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.removeItem", itemType.ToString());
                  break;
               case StorageLocation.LocalStorage:
                  await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.removeItem", itemType.ToString());
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
