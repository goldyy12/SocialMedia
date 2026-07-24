
using backend.DTOs;

namespace backend.Interfaces
{
    public interface IFollowService
    {
        Task<SendFollowResult> SendFollowRequestAsync(int userId, int targetUserId);
        Task<AcceptFollowResult> AcceptFollowRequestAsync(int userId, int followerId);
        Task<RemoveFollowerResult> RemoveFollowerAsync(int userId, int followerId);
        Task<UnfollowResult> UnfollowAsync(int userId, int targetUserId);
        Task<List<FollowRequestDto>> GetFollowRequestsAsync(int userId);
        Task<List<FollowerDto>> GetFollowersAsync(int userId);
        Task<List<FollowingDto>> GetFollowingAsync(int userId);
        Task<List<AvailableFriendDto>> GetAvailableFriendsAsync(int userId);
    }
}