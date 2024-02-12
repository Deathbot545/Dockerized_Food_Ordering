using Core.Services.User;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Food_Ordering_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserProfileApiController(IUserService userService)
        {
            _userService = userService;
        }

        // GET api/UserProfile
        [HttpGet("GetUserProfile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            // Log or print allClaims to see what claims are being received

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User Id is missing");
            }

            var userProfile = await _userService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                return NotFound();
            }

            return Ok(userProfile);
        }

        // PUT api/UserProfile
        [HttpPut]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileModel model)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isUpdated = await _userService.UpdateUserProfileAsync(userId, model);

            if (!isUpdated)
            {
                return BadRequest("Could not update user profile.");
            }

            return Ok("User profile updated successfully.");
        }
    }
}
