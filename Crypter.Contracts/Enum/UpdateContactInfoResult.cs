namespace Crypter.Contracts.Enum
{
    public enum UpdateContactInfoResult
    {
        Success,
        UserNotFound,
        EmailUnavailable,
        EmailInvalid,
        PasswordValidationFailed,
        ErrorResettingNotificationPreferences,
        UnknownError
    }
}
