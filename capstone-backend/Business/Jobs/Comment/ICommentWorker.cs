namespace capstone_backend.Business.Jobs.Comment
{
    public interface ICommentWorker
    {
        Task RecountPostAsync(int postId);
        Task RecountReplyAsync(int commentId);
    }
}
