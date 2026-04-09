using capstone_backend.Business.DTOs.Review;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class GetMyReviewRequestValidator : AbstractValidator<GetMyReviewRequest>
    {
        public GetMyReviewRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0");

            RuleFor(x => x.VenueId)
                .GreaterThan(0).WithMessage("VenueId phải lớn hơn 0")
                .When(x => x.VenueId.HasValue);

            RuleFor(x => x.Keyword)
                .MaximumLength(200).WithMessage("Từ khoá không được vượt quá 200 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Keyword));
        }
    }
}
