using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
    {
        public UpdatePostRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Thiếu nội dung");

            RuleFor(x => x.Visibility)
                .NotEmpty().WithMessage("Visibility không được để trống")
                .Must(BeValidVisibility).WithMessage("Visibility không hợp lệ. Chỉ chấp nhận PUBLIC, PRIVATE, COUPLE_ONLY");
            
            RuleForEach(x => x.Topic)
                .NotEmpty().WithMessage("Topic không được để trống")
                .Must(BeValidTopic).WithMessage("Topic không hợp lệ");
        }

        private bool BeValidVisibility(string visibility)
        {
            return Enum.TryParse<PostVisibility>(visibility, out _);
        }

        private bool BeValidTopic(string topic)
        {
            return InterestConstants.All.Any(x => x.Key.Equals(topic, StringComparison.OrdinalIgnoreCase));
        }
    }
}
