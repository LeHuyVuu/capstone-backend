using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : BaseController
    {

        public AdminController()
        {
           
        }

        [HttpGet]
        public async Task<IActionResult> GetRoleAdmin()
        {
            if (GetCurrentUserRole() != "ADMIN")
                return ForbiddenResponse("You do not have permission to access this resource");
            else 
                return OkResponse("You are an admin");
        }
    }
}
