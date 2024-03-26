using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using Food_Ordering_API.Models;
using Food_Ordering_API.ViewModels;
using Food_Ordering_API.DTO;

namespace Food_Ordering_Web.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpClient = new HttpClient(); // Or however you get your HttpClient
            _httpClientFactory = httpClientFactory;
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
            try
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
                        return RedirectToCurrentView(actionName, controllerName, roleName);
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
                    else if (!string.IsNullOrEmpty(errorResponse))
                    {
                        // If the error is not in the expected format, still show a generic message
                        ModelState.AddModelError(string.Empty, "User already exists or other registration error.");
                    }
                    return RedirectToCurrentView(actionName, controllerName, roleName);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred");
                throw;
            }
        }

        private IActionResult RedirectToCurrentView(string actionName, string controllerName, string roleName)
        {
            // Adjust the logic based on roleName and how you've set up your view paths
            if (roleName == "Restaurant")
            {
                // Directly returning the view as per the action method that loads the restaurant owner page
                return View("Regiser_Bussiness"); // Assuming the view file is in /Views/Home/Regiser_Bussiness.cshtml
            }
            else
            {
                // Directly returning the view as per the action method that loads the customer signup page
                return View("Signup"); // Assuming the view file is in /Views/Account/Register.cshtml or /Views/Account/Signup.cshtml based on your setup
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
            try
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
                        _logger.LogInformation("JWT token read.");
                        return await HandleLogin(user, responseObject.Token, outletId, tableId);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Could not deserialize response.");
                        ViewBag.ErrorMessage = "Could not deserialize response.";
                        _logger.LogInformation("JWT token read.");
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in MyAction");
                throw; // Rethrow the exception or handle it as needed
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
                UserDto userDto = new UserDto { Username = email, Password = "" };
                var json = JsonConvert.SerializeObject(userDto);
                _logger.LogInformation($"JSON Payload: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var fullUrl = $"{_apiBaseUrl}/Register/{role}";
                _logger.LogInformation($"Full URL: {fullUrl}");

                var apiResponse = await _httpClient.PostAsync(fullUrl, content);

                if (apiResponse == null)
                {
                    _logger.LogError("API Response is null");
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }

                if (!apiResponse.IsSuccessStatusCode)
                {
                    var errorContent = await apiResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"API Response is unsuccessful. StatusCode: {apiResponse.StatusCode}, Content: {errorContent}");

                    // Deserialize the error response to get error messages
                    var errorResult = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                    if (errorResult != null && errorResult.Errors != null)
                    {
                        foreach (var error in errorResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "User already exists or other registration error.");
                    }

           
                    string viewName = role == "Restaurant" ? "Regiser_Bussiness" : "Signup"; // Adjust based on your setup
                    return View(viewName); // Directly return to the view, keeping the error messages in ModelState
                }


                var apiResponseContent = await apiResponse.Content.ReadAsStringAsync();
                var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(apiResponseContent);

                if (responseObject == null)
                {
                    _logger.LogError("Could not deserialize API response.");
                    ModelState.AddModelError(string.Empty, "Could not deserialize response.");
                    return RedirectToAction("SuccessAction");
                }

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
        public IActionResult IntermediateRedirect(string returnUrl)
        {
            _logger.LogInformation("Intermediate redirect to ensure cookie is set.");
            return Redirect(returnUrl);
        }


        public async Task<IActionResult> HandleLogin(ApplicationUser user, string token, int? outletId = null, int? tableId = null)
        {
            try
            {
                _logger.LogInformation($"Starting login process for user {user.UserName}");

                // Decode JWT to get role and subscription status
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                var roleClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role);
                var isSubscribedClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "IsSubscribed"); // Get the IsSubscribed claim from the JWT

                if (roleClaim == null)
                {
                    _logger.LogError($"Role claim not found in JWT for user {user.UserName}");
                    return BadRequest("Role claim not found in JWT.");
                }

                // Ensure you convert the IsSubscribed string to a boolean, and handle null cases
                bool isSubscribed = isSubscribedClaim != null && bool.Parse(isSubscribedClaim.Value);

                var role = roleClaim.Value;
                _logger.LogInformation($"Role {role} found in JWT for user {user.UserName}");

                // Create claims, including the IsSubscribed claim
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserId", user.Id),
            new Claim("IsSubscribed", isSubscribed.ToString()) // Add IsSubscribed claim
        };

                // Create ClaimsIdentity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("Attempting to sign in user {UserName} with claims.", user.UserName);
                await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claimsIdentity.Claims);
                _logger.LogInformation("Sign in attempt for user {UserName} completed.", user.UserName);
                _logger.LogInformation($"User {user.UserName} authenticated: {HttpContext.User.Identity.IsAuthenticated}");


                // Check authentication status after SignInManager attempt
                if (User.Identity.IsAuthenticated)
                {
                    _logger.LogInformation("User {UserName} is authenticated after sign in attempt.", user.UserName);
                }
                else
                {
                    _logger.LogWarning("User {UserName} is NOT authenticated after sign in attempt.", user.UserName);
                }

                // Set the JWT token in a cookie
                Response.Cookies.Append("jwtCookie", token, new CookieOptions
                {
                    HttpOnly = true, // Recommended for security
                    Secure = true, // Set to true to enforce the cookie to be sent over HTTPS
                    SameSite = SameSiteMode.Lax, // Adjust based on your requirements
                    Expires = DateTime.UtcNow.AddMinutes(30) // Set the same expiry as your JWT token
                });

                _logger.LogInformation("JWT cookie set for user {UserName}.", user.UserName);
                foreach (var claim in HttpContext.User.Claims)
                {
                    _logger.LogInformation($"Claim type: {claim.Type}, Claim value: {claim.Value}");
                }


                // Redirect based on role
                if (outletId.HasValue && tableId.HasValue)
                {
                    return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");
                }
                else
                {
                    string finalUrl = Url.Action("Index", role); // Or whatever your target URL is
                    return RedirectToAction("IntermediateRedirect", new { returnUrl = finalUrl }); // Assuming you have an Index action for each role.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the login process for user {UserName}.", user.UserName);
                return View("Error"); // Ensure you have an Error view to handle exceptions gracefully
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

        [HttpPost]
        public async Task<IActionResult> UpdateUserProfile(UserProfileModel model)
        {
            // Extract user ID from the claims
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("User is not authenticated.");
            }
            var userId = userIdClaim.Value;

            // Construct the request to the API
            var requestUrl = "https://restosolutionssaas.com:7248/api/UserProfileApi/UpdateUserProfile";
            var httpClient = _httpClientFactory.CreateClient(); // Assuming you have HttpClientFactory injected

            // Retrieve the JWT token from the cookie named "jwtCookie"
            var token = HttpContext.Request.Cookies["jwtCookie"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("JWT token is missing.");
            }
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var updateModel = new
            {
                UserId = userId, // Make sure your API expects a UserId in the body
                Email = model.Email,
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber
            };

            var response = await httpClient.PatchAsync(requestUrl, new StringContent(JsonConvert.SerializeObject(updateModel), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                // Handle success
                // Handle success
                return RedirectToAction("Index", "UserProfile"); // Adjust "Index" and "UserProfile" as necessary
            }
            else
            {
                // Handle failure
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to update profile: {errorContent}");
                return View(model); // Return back to the edit profile view with error message
            }
        }



    }
}
