﻿
using Food_Ordering_API.DTO;
using Food_Ordering_API.Models;
using Food_Ordering_API.Services.AccountService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static Food_Ordering_API.Services.AccountService.AccountService;

namespace Food_Ordering_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountApiController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager; // <-- Add this line
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountApiController> _logger; 

        public AccountApiController(AccountService accountService, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, SignInManager<ApplicationUser> signInManager, ILogger<AccountApiController> logger) // <-- Add this
        {
            _accountService = accountService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _signInManager = signInManager; // <-- Add this
            _logger = logger;
        }

        [HttpPost("UpdateSubscriptionStatus")]
        public async Task<IActionResult> UpdateSubscriptionStatus([FromBody] UpdateSubscriptionStatusDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            user.IsSubscribed = model.IsSubscribed;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Subscription status updated successfully." });
            }
            else
            {
                return BadRequest(new { message = "Failed to update subscription status." });
            }
        }

        public class UpdateSubscriptionStatusDto
        {
            public string UserId { get; set; }
            public bool IsSubscribed { get; set; }
        }

        [HttpPost("Register/{roleName}")]
        public async Task<IActionResult> AddUser(string roleName, [FromBody] UserDto model)
        {
            var existingUser = await _accountService.FindUserAsync(model.Username);
            if (existingUser != null)
            {
                // User already exists
                return BadRequest(new { Message = "User already exists" });
            }

            var (success, errors, user) = await _accountService.AddUserAsync(model.Username, model.Password, roleName);
            if (success)
            {
                try
                {
                    var token = await GenerateJwtToken(user);
                    return Ok(new
                    {
                        Message = "Successfully logged in",
                        user = new { user.Id, user.UserName, user.Email, user.NormalizedUserName },
                        Token = token
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate JWT token for user {UserId}", user.Id);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing your request." });
                }
            }
            else
            {
                // Registration failed due to validation errors
                return BadRequest(new { Errors = errors });
            }
        }



        // Your API
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            try
            {
                var user = await _accountService.LoginAsync(model.UsernameOrEmail, model.Password);

                if (user != null)
                {
                    var token = await GenerateJwtToken(user);
                    return Ok(new
                    {
                        message = "Successfully logged in",
                        user = new { user.Id, user.UserName, user.Email, user.NormalizedUserName, /* other fields as required */ },
                        token = token
                    });
                }
            }
            catch (AccountService.UserNotFoundException)
            {
                return NotFound(new { message = "User not found" });
            }
            catch (AccountService.InvalidLoginException)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            return BadRequest(new { message = "An unknown error occurred" });
        }

        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] LoginDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.UsernameOrEmail))
            {
                return BadRequest("Invalid data.");
            }

            string email = model.UsernameOrEmail;

            // Check if the user exists in the database
            var user = await _accountService.FindUserAsync(email);

            if (user != null)
            {
                var token = await GenerateJwtToken(user);
                return Ok(new {
                    Message = "Successfully logged in",
                    user = user, // Include user details in the response
                    Token = token });
            }
            else
            {
                return BadRequest("User not registered.");
            }
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("IsSubscribed", user.IsSubscribed.ToString()) // Add subscription status as a claim
    };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30), // Customize the expiry as needed
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // API controller
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            // Invalidate token, remove from cache, or whatever needed
            // ...

            return Ok(new { message = "Successfully logged out" });
        }



    }
}
