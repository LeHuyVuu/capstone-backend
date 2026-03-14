using capstone_backend.Business.DTOs.Admin;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class AdminDashboardRequestValidator : AbstractValidator<AdminDashboardRequest>
{
    public AdminDashboardRequestValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, 2100)
            .When(x => x.Year.HasValue)
            .WithMessage("Year must be between 1900 and 2100");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage("Month must be between 1 and 12");

        RuleFor(x => x.Day)
            .InclusiveBetween(1, 31)
            .When(x => x.Day.HasValue)
            .WithMessage("Day must be between 1 and 31");

        RuleFor(x => x)
            .Must(x => !x.Day.HasValue || (x.Month.HasValue && x.Year.HasValue))
            .WithMessage("Day parameter requires both month and year");

        RuleFor(x => x)
            .Must(x => !x.Month.HasValue || x.Year.HasValue)
            .WithMessage("Month parameter requires year");

        RuleFor(x => x)
            .Must(x => IsValidDate(x.Year, x.Month, x.Day))
            .When(x => x.Day.HasValue && x.Month.HasValue && x.Year.HasValue)
            .WithMessage("Invalid date. Please check the day, month, and year values");
    }

    private bool IsValidDate(int? year, int? month, int? day)
    {
        if (!year.HasValue || !month.HasValue || !day.HasValue)
            return true;

        try
        {
            var date = new DateTime(year.Value, month.Value, day.Value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
