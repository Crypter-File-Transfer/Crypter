namespace Crypter.Contracts.Enum
{
    public enum InsertUserResult
    {
        Success,
        InvalidUsername,
        InvalidPassword,
        InvalidEmailAddress,
        UsernameTaken,
        EmailTaken
    }
}
