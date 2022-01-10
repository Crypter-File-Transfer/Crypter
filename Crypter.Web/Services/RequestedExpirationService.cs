using System;

namespace Crypter.Web.Services
{
    public interface IRequestedExpirationService
    {
        public DateTime ReturnRequestedExpirationFromRequestedExpirationInHours(int hours);
    }


    public class RequestedExpirationService : IRequestedExpirationService
    {
        public DateTime ReturnRequestedExpirationFromRequestedExpirationInHours(int hours)
        {
            return DateTime.UtcNow.AddHours(hours);
        }
    }
}
