namespace capstone_backend.Business.Jobs.Like
{
    public interface ILikeWorker
    {
        Task RecountPostLikeAsync(int postId);
    }
}
