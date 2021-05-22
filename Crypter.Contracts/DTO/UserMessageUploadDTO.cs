using System;
namespace Crypter.DataAccess.DTO
{
    public class UserMessageUploadDTO
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public string UntrustedName { get; set; }
        public int Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
