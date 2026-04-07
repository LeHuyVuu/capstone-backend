using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Category;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoryController : BaseController
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoryResponse>>), 200)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        if (page < 1)
            return BadRequestResponse("Số trang phải lớn hơn 0");

        if (pageSize < 1 || pageSize > 100)
            return BadRequestResponse("Kích thước trang phải trong khoảng từ 1 đến 100");

        _logger.LogInformation("Getting categories - Page: {Page}, PageSize: {PageSize}, IsActive: {IsActive}",
            page, pageSize, isActive);

        var result = await _categoryService.GetCategoriesAsync(page, pageSize, isActive);

        return OkResponse(result, $"Retrieved {result.Items.Count()} categories");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        _logger.LogInformation("Getting category by ID: {CategoryId}", id);

        var category = await _categoryService.GetCategoryByIdAsync(id);

        if (category == null)
            return NotFoundResponse($"Không tìm thấy danh mục có ID {id}");

        return OkResponse(category, "Category retrieved successfully");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Dữ liệu yêu cầu không hợp lệ");

        _logger.LogInformation("Creating new category: {CategoryName}", request.Name);

        try
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return CreatedResponse(category, "Category created successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create category");
            return BadRequestResponse(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Dữ liệu yêu cầu không hợp lệ");

        _logger.LogInformation("Updating category with ID: {CategoryId}", id);

        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);

            if (category == null)
                return NotFoundResponse($"Không tìm thấy danh mục có ID {id}");

            return OkResponse(category, "Category updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update category");
            return BadRequestResponse(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        _logger.LogInformation("Deleting category with ID: {CategoryId}", id);

        var result = await _categoryService.DeleteCategoryAsync(id);

        if (!result)
            return NotFoundResponse($"Không tìm thấy danh mục có ID {id}");

        return OkResponse(true, "Category deleted successfully");
    }
}
