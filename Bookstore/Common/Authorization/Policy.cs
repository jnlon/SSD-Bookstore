namespace Bookstore.Common.Authorization
{
    public static class Policy
    {
        public const string AdminOnly = nameof(AdminOnly);
        public const string MemberOnly = nameof(MemberOnly);
    }
}