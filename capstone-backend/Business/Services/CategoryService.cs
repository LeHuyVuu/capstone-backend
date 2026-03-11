using AutoMapper;
using capstone_backend.Business.DTOs.Category;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<CategoryResponse>> GetCategoriesAsync(int page, int pageSize, bool? isActive = null)
    {
        _logger.LogInformation("Getting categories - Page: {Page}, PageSize: {PageSize}, IsActive: {IsActive}", 
            page, pageSize, isActive);

        var (categories, totalCount) = await _unitOfWork.Categories.GetPagedAsync(
            page,
            pageSize,
            filter: c => !c.IsDeleted && (!isActive.HasValue || c.IsActive == isActive.Value),
            orderBy: q => q.OrderBy(c => c.Name)
        );

        var categoryResponses = _mapper.Map<List<CategoryResponse>>(categories);

        return new PagedResult<CategoryResponse>(categoryResponses, page, pageSize, totalCount);
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
    {
        _logger.LogInformation("Getting category by ID: {CategoryId}", id);

        var category = await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null || category.IsDeleted)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found or deleted", id);
            return null;
        }

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
    {
        _logger.LogInformation("Creating new category: {CategoryName}", request.Name);

        var existingCategory = await _unitOfWork.Categories.GetByNameAsync(request.Name);
        if (existingCategory != null)
        {
            _logger.LogWarning("Category with name '{CategoryName}' already exists", request.Name);
            throw new InvalidOperationException($"Category with name '{request.Name}' already exists");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Category created successfully with ID: {CategoryId}", category.Id);

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<CategoryResponse?> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        _logger.LogInformation("Updating category with ID: {CategoryId}", id);

        var category = await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null || category.IsDeleted)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found or deleted", id);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != category.Name)
        {
            var existingCategory = await _unitOfWork.Categories.GetByNameAsync(request.Name);
            if (existingCategory != null && existingCategory.Id != id)
            {
                _logger.LogWarning("Category with name '{CategoryName}' already exists", request.Name);
                throw new InvalidOperationException($"Category with name '{request.Name}' already exists");
            }
            category.Name = request.Name;
        }

      

        if (request.Description != null)
            category.Description = request.Description;

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} updated successfully", id);

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("Deleting category with ID: {CategoryId}", id);

        var category = await _unitOfWork.Categories.GetByIdAsync(id);

        if (category == null || category.IsDeleted)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found or already deleted", id);
            return false;
        }

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} deleted successfully", id);

        return true;
    }
}
