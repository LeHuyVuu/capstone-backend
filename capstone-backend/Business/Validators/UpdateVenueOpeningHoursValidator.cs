using capstone_backend.Business.DTOs.VenueLocation;
using FluentValidation;
using System.Text.RegularExpressions;

namespace capstone_backend.Business.Validators;

public class UpdateVenueOpeningHoursValidator : AbstractValidator<UpdateVenueOpeningHoursRequest>
{
    public UpdateVenueOpeningHoursValidator()
    {
        RuleFor(x => x.VenueLocationId)
            .GreaterThan(0).WithMessage("VenueLocationId phải lớn hơn 0");

        RuleFor(x => x.OpeningHours)
            .NotEmpty().WithMessage("Danh sách giờ mở cửa không được để trống");

        RuleForEach(x => x.OpeningHours).ChildRules(hour =>
        {
            hour.RuleFor(h => h.Day)
                .InclusiveBetween(2, 8)
                .WithMessage("Ngày phải từ 2 (Thứ 2) đến 8 (Chủ nhật)");

            hour.RuleFor(h => h.OpenTime)
                .Must(BeValidTimeFormat)
                .When(h => !h.IsClosed)
                .WithMessage("Giờ mở cửa không hợp lệ (format: HH:mm)");

            hour.RuleFor(h => h.CloseTime)
                .Must(BeValidTimeFormat)
                .When(h => !h.IsClosed)
                .WithMessage("Giờ đóng cửa không hợp lệ (format: HH:mm)");
        });
    }

    private bool BeValidTimeFormat(string? time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return false;

        if (!Regex.IsMatch(time, @"^([01]\d|2[0-3]):([0-5]\d)$"))
            return false;

        return TimeSpan.TryParse(time, out _);
    }
}
