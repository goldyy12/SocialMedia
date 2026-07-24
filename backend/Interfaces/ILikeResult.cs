namespace backend.Interfaces
{
    public enum LikeResult
    {
        Success,
        PostNotFound,
        AlreadyLiked
    }

    public enum UnlikeResult
    {
        Success,
        LikeNotFound
    }
}