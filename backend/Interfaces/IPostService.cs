using backend.DTOs;

namespace backend.Interfaces
{
    public interface IPostService
    {
        Task<PostResponseDto?> CreatePostAsync(int userId, PostDto dto);
        Task<List<PostResponseDto>> GetFeedAsync(int currentUserId);
        Task<List<PostResponseDto>> GetPostsByUserAsync(int userId);
        Task<bool?> DeletePostAsync(int postId, int userId); // null = not found, false = forbidden, true = deleted
        Task<bool?> UpdatePostAsync(int postId, int userId, PostDto dto);
    }
}