namespace Crypter.Contracts.Enum
{
    public enum InsertUserResult
    {
        Success,
        EmptyUsername,
        EmptyPassword,
        EmptyEmail,
        UsernameTaken,
        EmailTaken
    }
}
