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

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Exceptions;
using EasyMonads;
using Microsoft.JSInterop;

namespace Crypter.Web.Repositories;

public class BrowserRepository : IDeviceRepository<BrowserStorageLocation>
{
   public const string SessionStorageLiteral = "sessionStorage";
   public const string LocalStorageLiteral = "localStorage";

   private readonly IJSRuntime _jsRuntime;
   private readonly Dictionary<string, object> _inMemoryStorage;
   private readonly Dictionary<string, BrowserStorageLocation> _objectLocations;
   private readonly SemaphoreSlim _initializationSemaphore = new(1);
   private bool _initialized;
   private EventHandler _initializedEventHandler;

   public BrowserRepository(IJSRuntime jSRuntime)
   {
      _jsRuntime = jSRuntime;
      _inMemoryStorage = new Dictionary<string, object>();
      _objectLocations = new Dictionary<string, BrowserStorageLocation>();
   }

   public async Task InitializeAsync()
   {
      await _initializationSemaphore.WaitAsync().ConfigureAwait(false);
      try
      {
         if (!_initialized)
         {
            foreach (DeviceStorageObjectType item in Enum.GetValues(typeof(DeviceStorageObjectType)))
            {
               await InitializeItemFromLocalStorage(item);
               await InitializeItemFromSessionStorage(item);
            }
            _initialized = true;
            _initializedEventHandler?.Invoke(this, EventArgs.Empty);
         }
      }
      finally
      {
         _initializationSemaphore.Release();
      }
   }

   private void AssertInitialized()
   {
      if (!_initialized)
      {
         throw new NotInitializedException();
      }
   }

   private async Task InitializeItemFromLocalStorage(DeviceStorageObjectType itemType)
   {
      var value = await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", new object[] { itemType.ToString() });
      if (!string.IsNullOrEmpty(value))
      {
         _objectLocations.Add(itemType.ToString(), BrowserStorageLocation.LocalStorage);
      }
   }

   private async Task InitializeItemFromSessionStorage(DeviceStorageObjectType itemType)
   {
      var value = await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", new object[] { itemType.ToString() });
      if (!string.IsNullOrEmpty(value))
      {
         _objectLocations.Add(itemType.ToString(), BrowserStorageLocation.SessionStorage);
      }
   }

   public async Task<Maybe<T>> GetItemAsync<T>(DeviceStorageObjectType itemType)
   {
      AssertInitialized();
      string strItemType = itemType.ToString();
      if (_objectLocations.TryGetValue(strItemType, out var location))
      {
         ValueTask<string> jsonTask;
         switch (location)
         {
            case BrowserStorageLocation.Memory:
               return Maybe<T>.From((T)_inMemoryStorage[strItemType]);
            case BrowserStorageLocation.SessionStorage:
               jsonTask = _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.getItem", new object[] { strItemType });
               break;
            case BrowserStorageLocation.LocalStorage:
               jsonTask = _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.getItem", new object[] { strItemType });
               break;
            default:
               throw new NotImplementedException();
         }

         string json = await jsonTask;
         try
         {
            return JsonSerializer.Deserialize<T>(json);
         }
         catch (JsonException)
         {
            return default;
         }
      }
      return default;
   }

   public bool HasItem(DeviceStorageObjectType itemType)
   {
      AssertInitialized();
      return _objectLocations.ContainsKey(itemType.ToString());
   }

   public Maybe<BrowserStorageLocation> GetItemLocation(DeviceStorageObjectType itemType)
   {
      AssertInitialized();
      if (_objectLocations.TryGetValue(itemType.ToString(), out var location))
      {
         return location;
      }
      return Maybe<BrowserStorageLocation>.None;
   }

   public async Task<Unit> SetItemAsync<T>(DeviceStorageObjectType itemType, T value, BrowserStorageLocation location)
   {
      await RemoveItemAsync(itemType);
      string strItemType = itemType.ToString();
      switch (location)
      {
         case BrowserStorageLocation.Memory:
            _inMemoryStorage.Add(strItemType, value);
            break;
         case BrowserStorageLocation.SessionStorage:
            await _jsRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.setItem", new object[] { strItemType, JsonSerializer.Serialize(value) });
            break;
         case BrowserStorageLocation.LocalStorage:
            await _jsRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.setItem", new object[] { strItemType, JsonSerializer.Serialize(value) });
            break;
         default:
            throw new NotImplementedException();
      }

      _objectLocations.Add(strItemType, location);
      return Unit.Default;
   }

   public async Task<Unit> RemoveItemAsync(DeviceStorageObjectType itemType)
   {
      AssertInitialized();
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
      return Unit.Default;
   }

   public async Task<Unit> RecycleAsync()
   {
      AssertInitialized();
      foreach (DeviceStorageObjectType item in Enum.GetValues(typeof(DeviceStorageObjectType)))
      {
         await RemoveItemAsync(item);
      }
      return Unit.Default;
   }

   public event EventHandler InitializedEventHandler
   {
      add => _initializedEventHandler = (EventHandler)Delegate.Combine(_initializedEventHandler, value);
      remove => _initializedEventHandler = (EventHandler)Delegate.Remove(_initializedEventHandler, value);
   }
}