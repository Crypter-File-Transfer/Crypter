namespace Crypter.Contracts.Enum
{
    public enum InsertUserResult
    {
        Success,
        EmptyUsername,
        EmptyPassword,
        PasswordRequirementsNotMet,
        EmptyEmail,
        UsernameTaken,
        EmailTaken,
        InvalidBetaKey
    }
}
