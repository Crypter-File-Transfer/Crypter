using System;

namespace CrypterAPI.Models
{
   public class UploadItem
   {
      //unique key in database, will use GUID 
      public string ID { get; set; }
      // user id/ tag, null if anonymous
      public string UserID { get; set; }
      // item name/ title
      public string UntrustedName { get; set; }
      // file size in bytes
      public int Size { get; set; }
      // signature
      public string Signature { get; set; }
      // file time stamp
      public DateTime Created { get; set; }
      // expiration date
      public DateTime ExpirationDate { get; set;}

      //constructor sets TimeStamp upon instantiation
      public UploadItem()
      {
        this.Created = DateTime.UtcNow;
        this.ExpirationDate = DateTime.UtcNow.AddHours(24); 
      }
   }
}