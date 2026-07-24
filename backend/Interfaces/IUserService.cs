
using backend.DTOs;

namespace backend.Interfaces
{
    public interface IUserService
    {
        Task<List<UserSummaryDto>> GetAllUsersAsync(int currentUserId);
        Task<List<UserSummaryDto>> SearchUsersAsync(string query, int currentUserId);
        Task<UserProfileDto?> GetUserByIdAsync(int id, int currentUserId);
        Task<UserSummaryDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    }
}