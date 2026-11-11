using backend.DTOs;
using backend.Services;

namespace backend.Interfaces
{
    public interface IPostService
    {
        Task<PostResponseDto?> CreatePostAsync(int userId, PostDto dto);
        Task<List<PostResponseDto>> GetFeedAsync(int currentUserId, int? beforeId = null, int limit = PostService.DefaultPageSize);
        Task<List<PostResponseDto>> GetPostsByUserAsync(int userId, int currentUserId, int? beforeId = null, int limit = PostService.DefaultPageSize);
        Task<bool?> DeletePostAsync(int postId, int userId);
        Task<(UpdatePostResult Result, PostResponseDto? Post)> UpdatePostAsync(int postId, int userId, PostDto dto);
    }
}