using capstone_backend.Business.DTOs.Review;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class ReviewReplyRequestValidator : AbstractValidator<ReviewReplyRequest>
    {
        public ReviewReplyRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung phản hồi không được để trống")
                .MaximumLength(2000).WithMessage("Nội dung phản hồi không được vượt quá 2000 ký tự");
        }
    }
}
