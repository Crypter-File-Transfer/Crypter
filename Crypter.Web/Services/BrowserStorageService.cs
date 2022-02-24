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
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public enum BrowserStorageLocation
   {
      Memory,
      SessionStorage,
      LocalStorage
   }

   public enum BrowserStoredObjectType
   {
      UserSession,
      AuthenticationToken,
      RefreshToken,
      PlaintextX25519PrivateKey,
      PlaintextEd25519PrivateKey,
      EncryptedX25519PrivateKey,
      EncryptedEd25519PrivateKey
   }

   public class BrowserStorageService : IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation>
   {
      public const string SessionStorageLiteral = "sessionStorage";
      public const string LocalStorageLiteral = "localStorage";

      private readonly IJSRuntime _jsRuntime;

      private readonly Dictionary<string, object> _inMemoryStorage;
      private readonly Dictionary<string, BrowserStorageLocation> _objectLocations;

      public bool IsInitialized { get; private set; } = false;

      public BrowserStorageService(IJSRuntime jSRuntime)
      {
         _jsRuntime = jSRuntime;
         _inMemoryStorage = new Dictionary<string, object>();
         _objectLocations = new Dictionary<string, BrowserStorageLocation>();
      }

      public async Task InitializeAsync()
      {
         if (!IsInitialized)
         {
            foreach (BrowserStoredObjectType item in Enum.GetValues(typeof(BrowserStoredObjectType)))
            {
               await InitializeItemFromLocalStorage(item);
               await InitializeItemFromSessionStorage(item);
            }
            IsInitialized = true;
         }
      }

      private async Task InitializeItemFromLocalStorage(BrowserStoredObjectType itemType)
      {
         var value = await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", new object[] { itemType.ToString() });
         if (!string.IsNullOrEmpty(value))
         {
            _objectLocations.Add(itemType.ToString(), BrowserStorageLocation.LocalStorage);
         }
      }

      private async Task InitializeItemFromSessionStorage(BrowserStoredObjectType itemType)
      {
         var value = await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", new object[] { itemType.ToString() });
         if (!string.IsNullOrEmpty(value))
         {
            _objectLocations.Add(itemType.ToString(), BrowserStorageLocation.SessionStorage);
         }
      }

      public async Task<T> GetItemAsync<T>(BrowserStoredObjectType itemType)
      {
         if (_objectLocations.TryGetValue(itemType.ToString(), out var location))
         {
            string storedJson;
            switch (location)
            {
               case BrowserStorageLocation.Memory:
                  return (T)_inMemoryStorage[itemType.ToString()];
               case BrowserStorageLocation.SessionStorage:
                  storedJson = await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", new object[] { itemType.ToString() });
                  break;
               case BrowserStorageLocation.LocalStorage:
                  storedJson = await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", new object[] { itemType.ToString() });
                  break;
               default:
                  throw new NotImplementedException();
            }
            return JsonSerializer.Deserialize<T>(storedJson);
         }
         return default;
      }

      public bool HasItem(BrowserStoredObjectType itemType)
      {
         return _objectLocations.ContainsKey(itemType.ToString());
      }

      public BrowserStorageLocation GetItemLocation(BrowserStoredObjectType itemType)
      {
         return _objectLocations[itemType.ToString()];
      }

      public async Task SetItemAsync<T>(BrowserStoredObjectType itemType, T value, BrowserStorageLocation location)
      {
         await RemoveItemAsync(itemType);

         switch (location)
         {
            case BrowserStorageLocation.Memory:
               _inMemoryStorage.Add(itemType.ToString(), value);
               break;
            case BrowserStorageLocation.SessionStorage:
               await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.setItem", new object[] { itemType.ToString(), JsonSerializer.Serialize(value) });
               break;
            case BrowserStorageLocation.LocalStorage:
               await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.setItem", new object[] { itemType.ToString(), JsonSerializer.Serialize(value) });
               break;
            default:
               throw new NotImplementedException();
         }

         _objectLocations.Add(itemType.ToString(), location);
      }

      public async Task ReplaceItemAsync<T>(BrowserStoredObjectType itemType, T value)
      {
         var currentLocation = GetItemLocation(itemType);
         await SetItemAsync(itemType, value, currentLocation);
      }

      public async Task RemoveItemAsync(BrowserStoredObjectType itemType)
      {
         if (_objectLocations.TryGetValue(itemType.ToString(), out var location))
         {
            _objectLocations.Remove(itemType.ToString());
            switch (location)
            {
               case BrowserStorageLocation.Memory:
                  _inMemoryStorage.Remove(itemType.ToString());
                  break;
               case BrowserStorageLocation.SessionStorage:
                  await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.removeItem", itemType.ToString());
                  break;
               case BrowserStorageLocation.LocalStorage:
                  await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.removeItem", itemType.ToString());
                  break;
               default:
                  throw new NotImplementedException();
            }
         }
      }

      public async Task DisposeAsync()
      {
         foreach (BrowserStoredObjectType item in Enum.GetValues(typeof(BrowserStoredObjectType)))
         {
            await RemoveItemAsync(item);
         }
      }
   }
}
