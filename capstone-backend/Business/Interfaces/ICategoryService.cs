using capstone_backend.Business.DTOs.Category;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryResponse>> GetCategoriesAsync(int page, int pageSize, bool? isActive = null);
    Task<CategoryResponse?> GetCategoryByIdAsync(int id);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryResponse?> UpdateCategoryAsync(int id, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(int id);
}
