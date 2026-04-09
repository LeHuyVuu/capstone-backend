using capstone_backend.Business.DTOs.Post;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
    {
        public CreateCommentRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung bình luận không được để trống");
        }
    }
}
