using capstone_backend.Business.DTOs.SubscriptionPackage;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/admin-subscription-packages")]
    [ApiController]
    [Authorize(Roles = "ADMIN, admin")]
    public class AdminSubscriptionPackageController : BaseController
    {
        private readonly ISubscriptionPackageService _subscriptionPackageService;

        public AdminSubscriptionPackageController(ISubscriptionPackageService subscriptionPackageService)
        {
            _subscriptionPackageService = subscriptionPackageService;
        }

        /// <summary>
        /// Admin lấy danh sách subscription packages
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPackages([FromQuery] string? type = null)
        {
            try
            {
                var result = await _subscriptionPackageService.GetAdminSubscriptionPackagesAsync(type, includeDeleted: false);
                return OkResponse(result, "Lấy danh sách package thành công");
            }
            catch (ArgumentException ex)
            {
                return BadRequestResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Admin lấy chi tiết subscription package
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPackageById(int id)
        {
            try
            {
                var result = await _subscriptionPackageService.GetAdminSubscriptionPackageByIdAsync(id);
                if (result == null)
                {
                    return NotFoundResponse("Không tìm thấy package");
                }

                return OkResponse(result, "Lấy chi tiết package thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Admin tạo subscription package mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePackage([FromBody] CreateSubscriptionPackageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequestResponse("Dữ liệu không hợp lệ");
                }

                var result = await _subscriptionPackageService.CreateSubscriptionPackageAsync(request);
                return CreatedResponse(result, "Tạo package thành công");
            }
            catch (ArgumentException ex)
            {
                return BadRequestResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequestResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Admin cập nhật subscription package
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePackage(int id, [FromBody] UpdateSubscriptionPackageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequestResponse("Dữ liệu không hợp lệ");
                }

                var result = await _subscriptionPackageService.UpdateAdminSubscriptionPackageAsync(id, request);
                return OkResponse(result, "Cập nhật package thành công");
            }
            catch (InvalidOperationException ex)
            {
                return NotFoundResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Admin xóa subscription package
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePackage(int id)
        {
            try
            {
                await _subscriptionPackageService.DeleteSubscriptionPackageAsync(id);
                return OkResponse("Vô hiệu hóa package thành công");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequestResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
