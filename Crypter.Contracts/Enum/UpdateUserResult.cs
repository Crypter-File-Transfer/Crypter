namespace Crypter.Contracts.Enum
{
    public enum UpdateUserCredentialsResult
    {
        Success,
        UserNotFound,
        UsernameUnavailable,
        EmailUnavailable,
        PasswordValidationFailed
    }

    public enum UpdateUserPreferencesResult
    {
        Success,
        UserNotFound
    }
}
