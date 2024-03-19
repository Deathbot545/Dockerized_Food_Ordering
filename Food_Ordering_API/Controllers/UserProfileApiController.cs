using Core.DTO;
using Core.Services.User;
using Core.ViewModels;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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


        [HttpPatch("UpdateUserProfile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdateDTO model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User Id is missing");
            }

            var result = await _userService.UpdateUserProfileAsync(userId, model);
            if (!result)
            {
                return BadRequest("Could not update user profile.");
            }

            return Ok("User profile updated successfully.");
        }

        // PUT api/UserProfile

    }
}
