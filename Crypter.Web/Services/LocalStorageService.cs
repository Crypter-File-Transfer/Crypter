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
      EncryptedX25519PrivateKey,
      EncryptedEd25519PrivateKey,
      PlaintextX25519PrivateKey,
      PlaintextEd25519PrivateKey
   }

   public interface ILocalStorageService
   {
      Task Initialize();
      Task<T> GetItem<T>(StoredObjectType itemType);
      bool HasItem(StoredObjectType itemType);
      Task SetItem<T>(StoredObjectType itemType, T value, StorageLocation location);
      Task RemoveItem(StoredObjectType itemType);
      Task Dispose();
   }

   public class LocalStorageService : ILocalStorageService
   {
      private const string SessionStorageLiteral = "sessionStorage";
      private const string LocalStorageLiteral = "localStorage";

      private readonly IJSRuntime JSRuntime;

      private readonly Dictionary<string, object> InMemoryStorage;
      private readonly Dictionary<string, StorageLocation> ObjectLocations;

      public LocalStorageService(IJSRuntime jSRuntime)
      {
         JSRuntime = jSRuntime;
         InMemoryStorage = new Dictionary<string, object>();
         ObjectLocations = new Dictionary<string, StorageLocation>();
      }

      public async Task Initialize()
      {
         foreach (StoredObjectType item in Enum.GetValues(typeof(StoredObjectType)))
         {
            await InitializeItemFromLocalStorage(item);
            await InitializeItemFromSessionStorage(item);
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

      public async Task<T> GetItem<T>(StoredObjectType itemType)
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

      public async Task SetItem<T>(StoredObjectType itemType, T value, StorageLocation location)
      {
         await RemoveItem(itemType);
         ObjectLocations.Add(itemType.ToString(), location);
         switch (location)
         {
            case StorageLocation.InMemory:
               InMemoryStorage.Add(itemType.ToString(), value);
               break;
            case StorageLocation.SessionStorage:
               await JSRuntime.InvokeAsync<string>($"{SessionStorageLiteral}.setItem", itemType.ToString(), JsonSerializer.Serialize(value));
               break;
            case StorageLocation.LocalStorage:
               await JSRuntime.InvokeAsync<string>($"{LocalStorageLiteral}.removeItem", itemType.ToString(), JsonSerializer.Serialize(value));
               break;
            default:
               throw new NotImplementedException();
         }
      }

      public async Task RemoveItem(StoredObjectType itemType)
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

      public async Task Dispose()
      {
         foreach (StoredObjectType item in Enum.GetValues(typeof(StoredObjectType)))
         {
            await RemoveItem(item);
         }
      }
   }
}
