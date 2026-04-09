using capstone_backend.Business.DTOs.Review;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
    {
        public UpdateReviewRequestValidator()
        {
            RuleFor(x => x.VenueLocationId)
                .GreaterThan(0).WithMessage("VenueLocationId phải lớn hơn 0");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Điểm đánh giá phải nằm trong khoảng [1 - 5]");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung đánh giá không được để trống")
                .MaximumLength(2000).WithMessage("Nội dung đánh giá không được vượt quá 2000 ký tự");

            RuleFor(x => x.DeletedImageUrls)
                .Must(images => images == null || images.Count <= 3)
                .WithMessage("Số lượng ảnh xoá không được vượt quá 3");

            RuleFor(x => x.NewImages)
                .Must(images => images == null || images.Count <= 3)
                .WithMessage("Số lượng ảnh mới không được vượt quá 3");
        }
    }
}
