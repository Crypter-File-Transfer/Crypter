namespace Crypter.API.Logic
{
    public static class AuthRules
    {
        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password);
        }
    }
}
