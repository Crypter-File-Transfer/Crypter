using System;

namespace Crypter.Core.Interfaces
{
   public interface IUser
   {
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Email { get; set; }
      public byte[] PasswordHash { get; set; }
      public byte[] PasswordSalt { get; set; }
      public bool EmailVerified { get; set; }
      public DateTime Created { get; set; }
      public DateTime LastLogin { get; set; }
   }
}
