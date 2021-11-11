namespace Crypter.Web.Models
{
   public class EmailVerificationParams
   {
      public string Code { get; set; }
      public string Signature { get; set; }
   }
}
