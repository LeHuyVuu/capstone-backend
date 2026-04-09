using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class ModerationRequestValidator : AbstractValidator<ModerationRequest>
    {
        public ModerationRequestValidator()
        {
            RuleFor(x => x.Action)
                .Must(action => Enum.IsDefined(typeof(ModerationRequestAction), action))
                .WithMessage("Action chỉ chấp nhận: PUBLISH hoặc CANCEL");
        }
    }
}
