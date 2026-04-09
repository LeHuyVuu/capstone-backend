using capstone_backend.Business.DTOs.Review;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class CheckinRequestValidator : AbstractValidator<CheckinRequest>
    {
        public CheckinRequestValidator()
        {
            RuleFor(x => x.VenueLocationId)
                .GreaterThan(0).WithMessage("VenueLocationId phải lớn hơn 0");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90m, 90m)
                .WithMessage("Latitude phải nằm trong khoảng [-90, 90]");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180m, 180m)
                .WithMessage("Longitude phải nằm trong khoảng [-180, 180]");
        }
    }
}
