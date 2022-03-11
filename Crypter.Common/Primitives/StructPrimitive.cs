using System;

namespace Crypter.Common.Primitives
{
   public struct Foo<T>
   {
      public readonly T Value;

      public Foo()
      {
         throw new NotSupportedException();
      }

      private Foo(T value)
      {
         Value = value;
      }

      private void Validate()
      {
         if (!CheckValidation(Value))
         {
            throw new ArgumentException(nameof(Value));
         }
      }

      public static bool CheckValidation(T value)
      {
         if (value == null)
         {
            return false;
         }

         return true;
      }

      public static Foo<T> From(T value)
      {
         var x = new Foo<T>(value);
         x.Validate();
         return x;
      }
   }
}
