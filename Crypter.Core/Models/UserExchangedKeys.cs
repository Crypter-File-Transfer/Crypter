using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserExchangedKeys")]
   public class UserExchangedKeys : IUserExchangedKeys
   {
      public Guid Id { get; set; }
      public Guid Owner { get; set; }
      public Guid Target { get; set; }
      public string ReceiveKey { get; set; }
      public string SendKey { get; set; }
      public DateTime Created { get; set; }

      public UserExchangedKeys(Guid id, Guid owner, Guid target, string receiveKey, string sendKey, DateTime created)
      {
         Id = id;
         Owner = owner;
         Target = target;
         ReceiveKey = receiveKey;
         SendKey = sendKey;
         Created = created;
      }
   }
}
