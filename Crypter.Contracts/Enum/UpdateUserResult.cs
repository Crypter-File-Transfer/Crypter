namespace Crypter.Contracts.Enum
{
    public enum UpdateUserCredentialsResult
    {
        Success,
        NotFound,
        UsernameUnavailable,
        EmailUnavailable,
        PasswordValidationFailed
    }

    public enum UpdateUserPreferencesResult
    {
        Success,
        NotFound
    }
}
