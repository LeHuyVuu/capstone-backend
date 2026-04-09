using capstone_backend.Business.DTOs.Challenge;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class CoupleChallengeQueryValidator : AbstractValidator<CoupleChallengeQuery>
    {
        private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
        {
            "updatedatasc",
            "updatedatdesc",
            "joinedatasc",
            "joinedatdesc",
            "updatedatasc",
            "updatedatdesc",
            "joinedatasc",
            "joinedatdesc"
        };

        public CoupleChallengeQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0");

            RuleFor(x => x.Q)
                .MaximumLength(200).WithMessage("Từ khóa tìm kiếm không được vượt quá 200 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Q));

            RuleFor(x => x.To)
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("To phải lớn hơn hoặc bằng From")
                .When(x => x.From.HasValue && x.To.HasValue);

            RuleFor(x => x.Sort)
                .Must(BeValidSort)
                .When(x => !string.IsNullOrWhiteSpace(x.Sort))
                .WithMessage("Sort không hợp lệ. Chỉ chấp nhận updatedAtAsc, updatedAtDesc, joinedAtAsc, joinedAtDesc");
        }

        private bool BeValidSort(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                return true;
            }

            return AllowedSorts.Contains(sort.Trim().ToLowerInvariant());
        }
    }
}
