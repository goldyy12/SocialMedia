namespace backend.Models
{
    // Values stored in Follow.Status. Kept as string constants (rather than an enum)
    // so the existing database column and data need no migration.
    public static class FollowStatus
    {
        public const string Pending = "pending";
        public const string Accepted = "accepted";
    }
}
