namespace Bookstore.Common.Authorization
{
    // Policy identifier for page authorization
    public static class Policy
    {
        public const string AdminOnly = nameof(AdminOnly);
        public const string MemberOnly = nameof(MemberOnly);
    }
}