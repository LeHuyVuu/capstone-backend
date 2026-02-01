using AutoMapper;
using capstone_backend.Business.DTOs.Question;
using capstone_backend.Business.DTOs.QuestionAnswer;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class QuestionProfile : Profile
    {
        public QuestionProfile()
        {
            CreateMap<Question, QuestionResponse>()
                .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.QuestionAnswers));
            CreateMap<QuestionAnswer, QuestionAnswerResponse>();

            // Dto v2
            CreateMap<Question, TestQuestionResponse>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionAnswers));
            CreateMap<QuestionAnswer, TestAnswerOptionDto>();
        }
    }
}
