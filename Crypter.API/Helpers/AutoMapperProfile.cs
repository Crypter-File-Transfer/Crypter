using AutoMapper;
using Crypter.DataAccess.Models;
using Crypter.Contracts.Requests.Registered;

namespace Crypter.API.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, AuthenticateUserRequest>();
            CreateMap<RegisterUserRequest, User>();
            CreateMap<UpdateUserCredentialsRequest, User>();
        }
    }
}
