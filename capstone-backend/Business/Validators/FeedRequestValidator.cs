using capstone_backend.Business.DTOs.Post;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class FeedRequestValidator : AbstractValidator<FeedRequest>
    {
        public FeedRequestValidator()
        {
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0");

            RuleFor(x => x.Cursor)
                .GreaterThan(0).WithMessage("Cursor phải lớn hơn 0")
                .When(x => x.Cursor.HasValue);
        }
    }
}
