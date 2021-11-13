namespace Crypter.Contracts.Enum
{
    public enum InsertUserResult
    {
        Success,
        EmptyUsername,
        EmptyPassword,
        PasswordRequirementsNotMet,
        InvalidEmailAddress,
        UsernameTaken,
        EmailTaken
    }
}
