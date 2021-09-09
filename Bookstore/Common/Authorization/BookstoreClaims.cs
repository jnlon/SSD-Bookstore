namespace Bookstore.Common.Authorization
{
    // Constants for claim identifier strings used by ASP.NET Core Identity
    public static class BookstoreClaims
    {
        public const string UserId = nameof(UserId);
        public const string UserName = nameof(UserName);
        public const string Role = nameof(Role);
    }
}