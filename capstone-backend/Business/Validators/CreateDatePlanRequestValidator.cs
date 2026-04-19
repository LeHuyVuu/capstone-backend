using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class CreateDatePlanRequestValidator : AbstractValidator<CreateDatePlanRequest>
{
    public CreateDatePlanRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề lịch trình không được để trống")
            .MaximumLength(255).WithMessage("Tiêu đề lịch trình không được vượt quá 255 ký tự")
            .MinimumLength(3).WithMessage("Tiêu đề lịch trình phải ít nhất 3 ký tự");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.PlannedStartAt)
            .NotEmpty().WithMessage("Thời gian bắt đầu không được để trống")
            .Must(date => date > DateTime.UtcNow.AddSeconds(-5))
            .WithMessage("Thời gian bắt đầu phải là trong tương lai");

        RuleFor(x => x.PlannedEndAt)
            .NotEmpty().WithMessage("Thời gian kết thúc không được để trống")
            .GreaterThan(x => x.PlannedStartAt)
            .WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu");

        RuleFor(x => x.EstimatedBudget)
            .GreaterThan(0).WithMessage("Ngân sách dự kiến phải lớn hơn 0")
            .LessThanOrEqualTo(1000000000m).WithMessage("Ngân sách dự kiến không được vượt quá 1,000,000,000");

        RuleFor(x => x.DurationMode)
            .Must(mode => Enum.IsDefined(typeof(DatePlanDurationMode), mode))
            .WithMessage("Kiểu thời lượng lịch trình không hợp lệ");

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                if (request.DurationMode == DatePlanDurationMode.SAME_DAY)
                {
                    var startVn = request.PlannedStartAt;
                    var endVn = request.PlannedEndAt;

                    if (startVn.Date != endVn.Date)
                    {
                        context.AddFailure("DurationMode", "Lịch trình mặc định chỉ được tạo trong cùng một ngày");
                    }
                }
                else if (request.DurationMode == DatePlanDurationMode.WITHIN_24_HOURS)
                {
                    var duration = request.PlannedEndAt - request.PlannedStartAt;
                    if (duration > TimeSpan.FromHours(24))
                    {
                        context.AddFailure("DurationMode", "Lịch trình dạng 24 giờ không được vượt quá 24 giờ");
                    }
                }
            });

        RuleFor(x => x)
            .Must(x => (x.PlannedEndAt - x.PlannedStartAt) >= TimeSpan.FromHours(1))
            .WithMessage("Thời gian bắt đầu và kết thúc phải cách nhau ít nhất 1 giờ");
    }
}
