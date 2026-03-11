using AutoMapper;
using capstone_backend.Business.DTOs.Category;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryResponse>();
        CreateMap<CreateCategoryRequest, Category>();
    }
}
