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
   }
}
