namespace backend.Interfaces
{
    public interface ILikeService
    {
        Task<LikeResult> LikePostAsync(int postId, int userId);
        Task<UnlikeResult> UnlikePostAsync(int postId, int userId);
    }
}