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
            .WithMessage("Năm phải nằm trong khoảng từ 1900 đến 2100");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage("Tháng phải nằm trong khoảng từ 1 đến 12");

        RuleFor(x => x.Day)
            .InclusiveBetween(1, 31)
            .When(x => x.Day.HasValue)
            .WithMessage("Ngày phải nằm trong khoảng từ 1 đến 31");

        RuleFor(x => x)
            .Must(x => !x.Day.HasValue || (x.Month.HasValue && x.Year.HasValue))
            .WithMessage("Tham số ngày yêu cầu phải có cả tháng và năm");

        RuleFor(x => x)
            .Must(x => !x.Month.HasValue || x.Year.HasValue)
            .WithMessage("Tham số tháng yêu cầu phải có năm");

        RuleFor(x => x)
            .Must(x => IsValidDate(x.Year, x.Month, x.Day))
            .When(x => x.Day.HasValue && x.Month.HasValue && x.Year.HasValue)
            .WithMessage("Ngày không hợp lệ. Vui lòng kiểm tra lại giá trị ngày, tháng và năm");
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
