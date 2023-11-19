/*
 * Credit: Bartłomiej Iskrzycki
 * Url: https://brokul.dev/sending-files-and-additional-data-using-httpclient-in-net-core
 */

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Crypter.API.Contracts.ModelBinders;

public class FormDataJsonBinder : IModelBinder
{
   public Task BindModelAsync(ModelBindingContext bindingContext)
   {
      if (bindingContext == null)
      {
         throw new ArgumentNullException(nameof(bindingContext));
      }

      var modelName = bindingContext.ModelName;
      var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

      if (valueProviderResult == ValueProviderResult.None)
      {
         return Task.CompletedTask;
      }

      bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

      var value = valueProviderResult.FirstValue;
      if (string.IsNullOrEmpty(value))
      {
         return Task.CompletedTask;
      }

      try
      {
         var result = JsonSerializer.Deserialize(value, bindingContext.ModelType);
         bindingContext.Result = ModelBindingResult.Success(result);
      }
      catch (Exception)
      {
         bindingContext.Result = ModelBindingResult.Failed();
      }
      return Task.CompletedTask;
   }
}