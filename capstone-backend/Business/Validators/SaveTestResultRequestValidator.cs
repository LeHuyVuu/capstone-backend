using capstone_backend.Business.DTOs.PersonalityTest;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class SaveTestResultRequestValidator : AbstractValidator<SaveTestResultRequest>
    {
        public SaveTestResultRequestValidator()
        {
            RuleFor(x => x.Action)
                .Must(action => Enum.IsDefined(typeof(TestAction), action))
                .WithMessage("Action không hợp lệ. Chỉ chấp nhận SAVE_PROGRESS hoặc SUBMIT");

            RuleForEach(x => x.Answers)
                .SetValidator(new AnswerDtoValidator());

            RuleFor(x => x.Answers)
                .Must(HaveUniqueQuestionIds)
                .WithMessage("Danh sách Answers không được chứa trùng QuestionId")
                .When(x => x.Answers != null && x.Answers.Any());
        }

        private bool HaveUniqueQuestionIds(List<AnswerDto>? answers)
        {
            if (answers == null || answers.Count == 0)
            {
                return true;
            }

            var uniqueCount = answers
                .Select(x => x.QuestionId)
                .Distinct()
                .Count();

            return uniqueCount == answers.Count;
        }
    }

    public class AnswerDtoValidator : AbstractValidator<AnswerDto>
    {
        public AnswerDtoValidator()
        {
            RuleFor(x => x.QuestionId)
                .GreaterThan(0).WithMessage("QuestionId phải lớn hơn 0");

            RuleFor(x => x.AnswerId)
                .GreaterThan(0).WithMessage("AnswerId phải lớn hơn 0");
        }
    }
}
