using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredUserPublicSettingsResponse : BaseResponse
    {
        public UpdateUserPreferencesResult Result { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserPublicSettingsResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisteredUserPublicSettingsResponse(ResponseCode status) : base(status)
        { }

        public RegisteredUserPublicSettingsResponse(UpdateUserPreferencesResult result) : base(ResponseCode.Success)
        {
            Result = result;
        }
    }
}
