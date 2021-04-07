using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace CrypterAPI.Models
{
   public class UploadItem
   {
      //unique key in database, will use GUID 
      public long Id { get; set; }
      // item name/ title
      public string UntrustedName { get; set; }
      // user id/ tag, null if anonymous
      public string UserID { get; set; }
      // file size in Mb
      public float Size { get; set; }
      // file time stamp
      public DateTime TimeStamp { get; set; }

      internal CrypterDB Db { get; set; }

      //constructor sets TimeStamp upon instantiation
      public UploadItem()
      {
         TimeStamp = DateTime.UtcNow;
      }
      internal UploadItem(CrypterDB db)
      {
         Db = db;
      }
   }
}