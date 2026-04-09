using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class UpdateChallengeRequestValidator : AbstractValidator<UpdateChallengeRequest>
    {
        public UpdateChallengeRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề thử thách không được để trống")
                .MaximumLength(255).WithMessage("Tiêu đề thử thách không được vượt quá 255 ký tự");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả thử thách không được để trống")
                .MaximumLength(2000).WithMessage("Mô tả thử thách không được vượt quá 2000 ký tự");

            RuleFor(x => x.TriggerEvent)
                .NotEmpty().WithMessage("TriggerEvent không được để trống")
                .Must(BeValidTriggerEvent)
                .WithMessage("TriggerEvent không hợp lệ");

            RuleFor(x => x.GoalMetric)
                .NotEmpty().WithMessage("GoalMetric không được để trống")
                .Must(BeValidGoalMetric)
                .WithMessage("GoalMetric không hợp lệ");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status không được để trống")
                .Must(BeValidStatus)
                .WithMessage("Status không hợp lệ");

            RuleFor(x => x.RewardPoints)
                .GreaterThan(0).WithMessage("Điểm thưởng phải là số dương lớn hơn 0");

            RuleFor(x => x.TargetGoal)
                .GreaterThan(0).WithMessage("Mục tiêu số lượng phải lớn hơn 0")
                .When(x => !string.Equals(x.GoalMetric, ChallengeConstants.GoalMetrics.UNIQUE_LIST, StringComparison.OrdinalIgnoreCase));

            RuleFor(x => x.StartDate)
                .Must(x => !x.HasValue || x.Value >= DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("Ngày bắt đầu không được nằm trong quá khứ");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày bắt đầu phải trước ngày kết thúc")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }

        private bool BeValidTriggerEvent(string triggerEvent)
        {
            return Enum.TryParse<ChallengeTriggerEvent>(triggerEvent, out _);
        }

        private bool BeValidGoalMetric(string goalMetric)
        {
            return ChallengeConstants.AllowedGoalMetrics.Contains(goalMetric);
        }

        private bool BeValidStatus(string status)
        {
            return Enum.TryParse<ChallengeStatus>(status, out _);
        }
    }
}
