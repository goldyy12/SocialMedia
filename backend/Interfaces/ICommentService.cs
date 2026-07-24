using backend.DTOs;

namespace backend.Interfaces
{
    public interface ICommentService
    {
        Task<List<CommentResponseDto>?> GetCommentsForPostAsync(int postId);
        Task<CommentResponseDto?> AddCommentAsync(int postId, int userId, CommentDto dto);
        Task<bool?> DeleteCommentAsync(int commentId, int userId); // null = not found, false = forbidden, true = deleted
        Task<CommentResponseDto?> EditCommentAsync(int commentId, int userId, CommentDto dto);
    }
}