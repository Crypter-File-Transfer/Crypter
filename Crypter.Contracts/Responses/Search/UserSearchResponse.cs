using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using System.Collections.Generic;

namespace Crypter.Contracts.Responses.Search
{
    public class UserSearchResponse : BaseResponse
    {
        public IEnumerable<UserSearchResultDTO> Result { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UserSearchResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UserSearchResponse(ResponseCode status) : base(status)
        { }


        public UserSearchResponse(IEnumerable<UserSearchResultDTO> result) : base(ResponseCode.Success)
        {
            Result = result;
        }
    }
}
