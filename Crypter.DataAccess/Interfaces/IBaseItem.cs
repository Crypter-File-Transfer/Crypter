using System;

namespace Crypter.DataAccess.Interfaces
{
    public interface IBaseItem
    {
        Guid Id { get; set; }
        Guid Sender { get; set; }
        int Size { get; set; }
        string CipherTextPath { get; set; }
        string SignaturePath { get; set; }
        byte[] ServerIV { get; set; }
        byte[] ServerDigest { get; set; }
        DateTime Created { get; set; }
        DateTime Expiration { get; set; }
    }
}
