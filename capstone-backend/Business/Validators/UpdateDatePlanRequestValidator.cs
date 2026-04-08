using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class UpdateDatePlanRequestValidator : AbstractValidator<UpdateDatePlanRequest>
{
    public UpdateDatePlanRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(255).WithMessage("Tiêu đề lịch trình không được vượt quá 255 ký tự")
            .MinimumLength(3).WithMessage("Tiêu đề lịch trình phải ít nhất 3 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.PlannedStartAt)
            .Must(date => date == null || date.Value > DateTime.UtcNow.AddSeconds(-5))
            .WithMessage("Thời gian bắt đầu phải là trong tương lai");

        RuleFor(x => x.PlannedEndAt)
            .Must((request, endDate) => 
                !endDate.HasValue || !request.PlannedStartAt.HasValue || endDate.Value > request.PlannedStartAt.Value)
            .WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu");

        RuleFor(x => x.EstimatedBudget)
            .GreaterThan(0).WithMessage("Ngân sách dự kiến phải lớn hơn 0")
            .LessThanOrEqualTo(1000000000m).WithMessage("Ngân sách dự kiến không được vượt quá 1,000,000,000")
            .When(x => x.EstimatedBudget.HasValue);

        RuleFor(x => x.DurationMode)
            .Must(mode => mode == null || Enum.IsDefined(typeof(DatePlanDurationMode), mode.Value))
            .WithMessage("Kiểu thời lượng lịch trình không hợp lệ");

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                // Only validate if both dates are provided
                if (request.PlannedStartAt.HasValue && request.PlannedEndAt.HasValue)
                {
                    if (request.DurationMode == DatePlanDurationMode.SAME_DAY)
                    {
                        var startVn = request.PlannedStartAt.Value;
                        var endVn = request.PlannedEndAt.Value;

                        if (startVn.Date != endVn.Date)
                        {
                            context.AddFailure("DurationMode", "Lịch trình mặc định chỉ được tạo trong cùng một ngày");
                        }
                    }
                    else if (request.DurationMode == DatePlanDurationMode.WITHIN_24_HOURS)
                    {
                        var duration = request.PlannedEndAt.Value - request.PlannedStartAt.Value;
                        if (duration > TimeSpan.FromHours(24))
                        {
                            context.AddFailure("DurationMode", "Lịch trình dạng 24 giờ không được vượt quá 24 giờ");
                        }
                    }
                }
            });
    }
}
