using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Core.Utilities;
using Core.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Google;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Net;
using Azure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Services.Users;

namespace Food_Ordering_API.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpClient = new HttpClient(); // Or however you get your HttpClient
            _apiBaseUrl = $"{configuration.GetValue<string>("ApiBaseUrl")}api/AccountApi";  // Modify it here
            _logger = logger;
        }

        [HttpGet]
        public IActionResult SpecialLogin(int outletId, int tableId)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");
            }
            else
            {
                ViewBag.OutletId = outletId;
                ViewBag.TableId = tableId;
                return View("~/Views/Account/SpecialLogin.cshtml"); // Make sure you have a view named "SpecialLogin"
            }
        }
        [HttpGet]
        public IActionResult Register()
        {
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"].ToString();
            }
            return View("~/Views/Account/Signup.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(string username, string password, string roleName)
        {
            string apiEndpoint = $"Register/{roleName}";

            return await AddUserToApi(apiEndpoint, username, password, "YourActionName", "YourControllerName",roleName);
        }


        private async Task<IActionResult> AddUserToApi(string apiEndpoint, string username, string password, string actionName, string controllerName, string roleName)
        {
            var userDto = new { Username = username, Password = password };
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/{apiEndpoint}",
                new StringContent(System.Text.Json.JsonSerializer.Serialize(userDto), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent);
                if (responseObject != null)
                {
                    // Create a new ApplicationUser object
                    ApplicationUser user = new ApplicationUser
                    {
                        Id = responseObject.User.Id,
                        UserName = responseObject.User.UserName,
                        Email = responseObject.User.Email
                        // Populate other necessary fields
                    };

                    return await HandleLogin(user, responseObject.Token);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Token was not provided.");
                    return Content("An error occurred.");
                }
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                var errorResult = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorResponse);
                if (errorResult != null && errorResult.Errors != null)
                {
                    foreach (var error in errorResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }
                // Return the view directly with the model errors
                return View("Signup"); // Assuming "Register" is the name of your view
            }

        }

        public IActionResult Login()
        {
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"].ToString();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string actionName, string controllerName, int? outletId = null, int? tableId = null)
        {
            var loginDto = new LoginDto
            {
                UsernameOrEmail = model.UserName, // Ensure this is the user's email.
                Password = model.Password
            };

            // Using System.Text.Json for serialization
            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(loginDto);
            var httpResponse = await _httpClient.PostAsync(
                $"{_apiBaseUrl}/Login",
                new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent);

                _logger.LogInformation($"Raw API Response: {responseContent}");
                if (responseObject != null)
                {
                    // Create a new ApplicationUser object
                    ApplicationUser user = new ApplicationUser
                    {
                        Id = responseObject.User.Id,
                        UserName = responseObject.User.UserName,
                        Email = responseObject.User.Email
                        // Populate other necessary fields
                    };

                    return await HandleLogin(user, responseObject.Token, outletId, tableId);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Could not deserialize response.");
                    ViewBag.ErrorMessage = "Could not deserialize response.";
                }
            }
            else
            {
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                var errorResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);

                if (errorResponse != null && errorResponse.ContainsKey("message"))
                {
                    ViewBag.ErrorMessage = errorResponse["message"];
                }
                else
                {
                    ViewBag.ErrorMessage = "An unknown error occurred.";
                }
            }

            return View(model);
        }

        public class LoginResponse
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("user")]
            public UserResponse User { get; set; }

            [JsonPropertyName("token")]
            public string Token { get; set; }
        }

        public class UserResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("userName")]
            public string UserName { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            // Add other properties as per API response
        }



        [HttpGet]
        public IActionResult ExternalRegisterOrLogin(string role = null, int? outletId = null, int? tableId = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { role = role, outletId = outletId, tableId = tableId })
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }


        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string role = null, int? outletId = null, int? tableId = null)
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (result?.Succeeded == true)
            {
                // User is authenticated
                _logger.LogInformation("User authenticated.");
            }
            else
            {
                // User is not authenticated
                _logger.LogError($"Authentication failed. Reason: {result?.Failure?.Message}");
                return Unauthorized(); // or some other action to handle failure
            }


            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            if (claims == null)
            {
                return BadRequest("No claims found.");
            }

            string email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email claim missing.");
            }

            if (!string.IsNullOrEmpty(role))
            {
                // Your existing UserDto creation
                UserDto userDto = new UserDto { Username = email, Password = "" };

                var json = JsonConvert.SerializeObject(userDto);
                _logger.LogInformation($"JSON Payload: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var fullUrl = $"{_apiBaseUrl}/Register/{role}";
                _logger.LogInformation($"Full URL: {fullUrl}");

                var apiResponse = await _httpClient.PostAsync(fullUrl, content);

                if (apiResponse != null)
                {
                    var apiResponseContent = await apiResponse.Content.ReadAsStringAsync();
                    if (apiResponse.IsSuccessStatusCode)
                    {
                        var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(apiResponseContent);
                        if (responseObject != null)
                        {
                            // Create a new ApplicationUser object
                            ApplicationUser user = new ApplicationUser
                            {
                                Id = responseObject.User.Id,
                                UserName = responseObject.User.UserName,
                                Email = responseObject.User.Email
                                // Populate other necessary fields
                            };

                            return await HandleLogin(user, responseObject.Token, outletId, tableId);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Could not deserialize response.");
                            return RedirectToAction("SuccessAction");
                        }
                    }
                    else if (apiResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var error = await apiResponse.Content.ReadAsStringAsync();
                        var errorObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(error);

                        if (errorObject != null && errorObject.ContainsKey("message"))
                        {
                            TempData["ErrorMessage"] = errorObject["message"];
                        }

                        return RedirectToAction("Register");
                    }
                    else
                    {
                        _logger.LogError("API Response is unsuccessful");
                        return StatusCode((int)HttpStatusCode.InternalServerError);
                    }
                }
                else
                {
                    _logger.LogError("API Response is null");
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }


            }
            else
            {
                // Login flow
                LoginDto loginDto = new LoginDto { UsernameOrEmail = email, Password = "" };
                var json = JsonConvert.SerializeObject(loginDto);
                _logger.LogInformation($"JSON Payload: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var fullUrl = $"{_apiBaseUrl}/GoogleLogin";
                _logger.LogInformation($"Full URL: {fullUrl}");
                var response = await _httpClient.PostAsync(fullUrl, content);
                //...
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent);
                    if (responseObject != null)
                    {
                        // Create a new ApplicationUser object
                        ApplicationUser user = new ApplicationUser
                        {
                            Id = responseObject.User.Id,
                            UserName = responseObject.User.UserName,
                            Email = responseObject.User.Email
                            // Populate other necessary fields
                        };

                        return await HandleLogin(user, responseObject.Token, outletId, tableId);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Could not deserialize response.");
                    }
                    return RedirectToAction("SuccessAction");
                }
                //...

                else
                {
                    ModelState.AddModelError(string.Empty, "User not registered or invalid credentials.");
                    TempData["ErrorMessage"] = "User not registered or invalid credentials."; // Using TempData to store the error message
                    return RedirectToAction("Login", "Account"); // Redirecting to Login action in Account controller
                }
            }

        }
        public async Task<IActionResult> HandleLogin(ApplicationUser user, string token, int? outletId = null, int? tableId = null)
        {
            // Decode JWT to get role
            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(token) as JwtSecurityToken;

            // Check for role claim safely
            var roleClaim = tokenS.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role);
            if (roleClaim == null)
            {
                _logger.LogError("Role claim not found in JWT.");
                return BadRequest("Role claim not found in JWT.");
            }

            var role = roleClaim.Value;

            // Create claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, role),
        new Claim("UserId", user.Id)
    };

            // Create ClaimsIdentity
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Use SignInManager to sign in the user
            await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claimsIdentity.Claims);

            // Set the JWT token in a cookie
            Response.Cookies.Append("jwtCookie", token, new CookieOptions
            {
                HttpOnly = true, // Recommended for security
                Secure = true, // Send the cookie over HTTPS only
                SameSite = SameSiteMode.Strict, // Recommended for security
                Expires = DateTime.UtcNow.AddMinutes(30) // Set the same expiry as your JWT token
            });

            // Redirect based on role
            if (outletId.HasValue && tableId.HasValue)
            {
                return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");
            }
            else
            {
                return RedirectToAction("Index", role); // Assuming you have an Index action for each role.
            }
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // HTTP POST to the API to invalidate the token or perform other logout activities
            var httpResponse = await _httpClient.PostAsync($"{_apiBaseUrl}/Logout", null);

            if (httpResponse.IsSuccessStatusCode)
            {
                // Remove JWT token from HttpOnly cookie
                Response.Cookies.Delete("jwtCookie");

                // Use SignInManager to sign out the user
                await _signInManager.SignOutAsync();

                // Redirect to another page (e.g., login page)
                return RedirectToAction("Login", "Account");
            }
            else
            {
                // If you wish, you can read the error message from API and display it
                var error = await httpResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to logout: {error}");

                // Return to the same or different view as needed
                return View();
            }
        }

        public class ErrorResponse
        {
            public string Message { get; set; }
            public IEnumerable<string> Errors { get; set; }
        }


    }
}
