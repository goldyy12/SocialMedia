// Interfaces/FollowResults.cs
namespace backend.Interfaces
{
    public enum SendFollowResult { Success, CannotFollowSelf, AlreadySent }
    public enum AcceptFollowResult { Success, NotFound, AlreadyAccepted }
    public enum RemoveFollowerResult { Success, NotFound }
    public enum UnfollowResult { Success, NotFound }
}