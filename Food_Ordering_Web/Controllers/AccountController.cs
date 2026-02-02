using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using Food_Ordering_API.ViewModels;

namespace Food_Ordering_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        private readonly Uri? _apiBaseUri;          // e.g. http://localhost:5100/
        private readonly Uri? _accountApiBaseUri;   // e.g. http://localhost:5100/api/AccountApi/

        private bool DemoEnabled => _configuration.GetValue<bool>("Demo:Enabled");

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

            var apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");

            if (!string.IsNullOrWhiteSpace(apiBaseUrl) &&
                Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var root))
            {
                _apiBaseUri = root;
                _accountApiBaseUri = new Uri(root, "api/AccountApi/");
            }

            _logger.LogInformation("DemoEnabled: {DemoEnabled}", DemoEnabled);
            _logger.LogInformation("ApiBaseUrl: {ApiBaseUrl}", apiBaseUrl ?? "(null)");
            _logger.LogInformation("AccountApiBaseUrl: {AccountApiBaseUrl}", _accountApiBaseUri?.ToString() ?? "(null)");
        }

        // -----------------------------
        // Demo Users
        // -----------------------------
        private sealed class DemoUser
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public string Role { get; set; } = "Customer";
            public bool IsSubscribed { get; set; } = false;
        }

        private List<DemoUser> GetDemoUsers()
        {
            var users = _configuration.GetSection("Demo:Users").Get<List<DemoUser>>();
            return users ?? new List<DemoUser>();
        }

        private DemoUser? FindDemoUser(string username, string password)
        {
            return GetDemoUsers()
                .FirstOrDefault(u =>
                    string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                    u.Password == password);
        }

        // -----------------------------
        // Routes
        // -----------------------------
        [HttpGet]
        public IActionResult SpecialLogin(int outletId, int tableId)
        {
            return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"]!.ToString();

            return View("~/Views/Account/Signup.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(string username, string password, string roleName)
        {
            if (DemoEnabled)
            {
                // Demo: auto-create/sign-in user without API
                var demo = new DemoUser
                {
                    Username = username,
                    Password = password,
                    Role = string.IsNullOrWhiteSpace(roleName) ? "Customer" : roleName,
                    IsSubscribed = roleName == "Restaurant" // simple rule: restaurant subscribed
                };

                await SignInDemoUser(demo);
                return RedirectToRoleHome(demo.Role);
            }

            if (_accountApiBaseUri == null)
                return StatusCode((int)HttpStatusCode.InternalServerError, "ApiBaseUrl is missing/invalid.");

            var apiEndpoint = $"Register/{roleName}";
            return await AddUserToApi(apiEndpoint, username, password, roleName);
        }

        private async Task<IActionResult> AddUserToApi(string apiEndpoint, string username, string password, string roleName)
        {
            try
            {
                var http = _httpClientFactory.CreateClient();

                var userDto = new { Username = username, Password = password };
                var body = new StringContent(System.Text.Json.JsonSerializer.Serialize(userDto), Encoding.UTF8, "application/json");

                var url = new Uri(_accountApiBaseUri!, apiEndpoint);
                var response = await http.PostAsync(url, body);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent);

                    if (!string.IsNullOrWhiteSpace(responseObject?.Token))
                        return await HandleLogin(responseObject.Token);

                    ModelState.AddModelError(string.Empty, "Token was not provided.");
                    return RedirectToCurrentView(roleName);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var errorResult = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(errorResponse);

                    if (errorResult?.Errors != null)
                    {
                        foreach (var error in errorResult.Errors)
                            ModelState.AddModelError(string.Empty, error);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Registration failed.");
                    }

                    return RedirectToCurrentView(roleName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                return View("Error");
            }
        }

        private IActionResult RedirectToCurrentView(string roleName)
        {
            if (roleName == "Restaurant")
                return View("Regiser_Bussiness");

            return View("Signup");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View("/Views/Account/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string actionName, string controllerName, int? outletId = null, int? tableId = null)
        {
            if (DemoEnabled)
            {
                var demoUser = FindDemoUser(model.UserName, model.Password);
                if (demoUser == null)
                {
                    ViewBag.ErrorMessage = "Invalid demo credentials.";
                    return View(model);
                }

                await SignInDemoUser(demoUser);

                if (outletId.HasValue && tableId.HasValue)
                    return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");

                return RedirectToRoleHome(demoUser.Role);
            }

            if (_accountApiBaseUri == null)
            {
                ViewBag.ErrorMessage = "ApiBaseUrl is missing/invalid.";
                return View(model);
            }

            try
            {
                var http = _httpClientFactory.CreateClient();

                var loginDto = new LoginDto
                {
                    UsernameOrEmail = model.UserName,
                    Password = model.Password
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(loginDto);
                var url = new Uri(_accountApiBaseUri, "Login");

                var httpResponse = await http.PostAsync(url,
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent);

                    if (!string.IsNullOrWhiteSpace(responseObject?.Token))
                        return await HandleLogin(responseObject.Token, outletId, tableId);

                    ViewBag.ErrorMessage = "Could not deserialize response token.";
                    return View(model);
                }
                else
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    // Keep it simple: show server message if exists
                    try
                    {
                        var errorResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
                        if (errorResponse != null && errorResponse.TryGetValue("message", out var msg))
                            ViewBag.ErrorMessage = msg;
                        else
                            ViewBag.ErrorMessage = "Login failed.";
                    }
                    catch
                    {
                        ViewBag.ErrorMessage = "Login failed.";
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return View("Error");
            }
        }

        // -----------------------------
        // Google Auth
        // -----------------------------
        [HttpGet]
        public IActionResult ExternalRegisterOrLogin(string role = null, int? outletId = null, int? tableId = null)
        {
            if (DemoEnabled)
            {
                // Demo: pretend Google login worked
                var demo = new DemoUser
                {
                    Username = "google.demo@local.test",
                    Password = "x",
                    Role = string.IsNullOrWhiteSpace(role) ? "Customer" : role,
                    IsSubscribed = role == "Restaurant"
                };

                return RedirectToAction(nameof(DemoGoogleCallback), new { role = demo.Role, outletId, tableId });
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { role = role, outletId = outletId, tableId = tableId })
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> DemoGoogleCallback(string role, int? outletId = null, int? tableId = null)
        {
            var demo = new DemoUser
            {
                Username = "google.demo@local.test",
                Password = "x",
                Role = string.IsNullOrWhiteSpace(role) ? "Customer" : role,
                IsSubscribed = role == "Restaurant"
            };

            await SignInDemoUser(demo);

            if (outletId.HasValue && tableId.HasValue)
                return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");

            return RedirectToRoleHome(demo.Role);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string role = null, int? outletId = null, int? tableId = null)
        {
            if (DemoEnabled)
            {
                // fallback safety
                await SignInDemoUser(new DemoUser { Username = "google.demo@local.test", Role = role ?? "Customer", IsSubscribed = role == "Restaurant" });
                return RedirectToRoleHome(role ?? "Customer");
            }

            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (result?.Succeeded != true)
            {
                _logger.LogError("Google auth failed: {Reason}", result?.Failure?.Message);
                return Unauthorized();
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            if (claims == null)
                return BadRequest("No claims found.");

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email claim missing.");

            if (_accountApiBaseUri == null)
                return StatusCode((int)HttpStatusCode.InternalServerError, "ApiBaseUrl is missing/invalid.");

            var http = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(role))
            {
                // register flow
                var userDto = new UserDto { Username = email, Password = "" };
                var json = JsonConvert.SerializeObject(userDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = new Uri(_accountApiBaseUri, $"Register/{role}");
                var apiResponse = await http.PostAsync(url, content);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Registration with Google failed (maybe user exists).";
                    return RedirectToAction("Register");
                }

                var apiResponseContent = await apiResponse.Content.ReadAsStringAsync();
                var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(apiResponseContent);

                return await HandleLogin(responseObject?.Token ?? "", outletId, tableId);
            }
            else
            {
                // login flow
                var loginDto = new LoginDto { UsernameOrEmail = email, Password = "" };
                var json = JsonConvert.SerializeObject(loginDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = new Uri(_accountApiBaseUri, "GoogleLogin");
                var response = await http.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(responseContent);

                    return await HandleLogin(responseObject?.Token ?? "", outletId, tableId);
                }

                TempData["ErrorMessage"] = "User not registered or invalid credentials.";
                return RedirectToAction("Login", "Account");
            }
        }

        // -----------------------------
        // Sign in helpers
        // -----------------------------
        private IActionResult RedirectToRoleHome(string role)
        {
            role = (role ?? "").Trim();

            // Normalize common variants from API
            var normalized = role.ToLowerInvariant();

            var controller = normalized switch
            {
                "customer" => "Customer",
                "user" => "Customer",
                "client" => "Customer",

                "restaurant" => "Restaurant",
                "owner" => "Restaurant",

                "admin" => "Admin",

                _ => "Home"
            };

            return RedirectToAction("Index", controller);
        }


        private async Task SignInDemoUser(DemoUser user)
        {
            var isDev = HttpContext.Request.IsHttps == false; // if not https, treat like dev cookie rules
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("IsSubscribed", user.IsSubscribed.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                });

            // Optional dummy JWT cookie (so parts of UI expecting it don't crash)
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev ? true : false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            };
            Response.Cookies.Append("jwtCookie", "demo-token", cookieOptions);
        }

        public async Task<IActionResult> HandleLogin(string token, int? outletId = null, int? tableId = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.ErrorMessage = "Missing token.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // ✅ USER ID: support common claim names
                var userId =
                    jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogError("User ID claim missing in JWT. Claims: {Claims}",
                        string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}={c.Value}")));
                    ViewBag.ErrorMessage = "Login token missing user id claim.";
                    return RedirectToAction("Login", "Account");
                }

                // ✅ NAME: support common claim names
                var userName =
                    jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                    ?? "";

                // ✅ ROLE: support common role claim names
                var role =
                    jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "roles")?.Value
                    ?? "Customer"; // safe default

                // ✅ SUBSCRIPTION
                var isSubscribedValue =
                    jwtToken.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "isSubscribed")?.Value
                    ?? "false";

                var isSubscribed = bool.TryParse(isSubscribedValue, out var s) && s;

                var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, userName),
    new Claim(ClaimTypes.Role, role),
    new Claim(ClaimTypes.NameIdentifier, userId),
    new Claim("IsSubscribed", isSubscribed.ToString())
};

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    });

                // Cookie for JWT
                var secure = HttpContext.Request.IsHttps;
                Response.Cookies.Append("jwtCookie", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = secure,
                    SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });

                // keep your existing outlet/table redirect
                if (outletId.HasValue && tableId.HasValue)
                    return Redirect($"/Order/Menu?outletId={outletId}&tableId={tableId}");

                // ✅ map role -> controller safely
                return RedirectToRoleHome(role);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login token handling failed.");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // In demo mode, do NOT call API
            if (!DemoEnabled && _accountApiBaseUri != null)
            {
                try
                {
                    var http = _httpClientFactory.CreateClient();
                    await http.PostAsync(new Uri(_accountApiBaseUri, "Logout"), null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API logout call failed (ignored).");
                }
            }

            Response.Cookies.Delete("jwtCookie");
            return RedirectToAction("Login", "Account");
        }

        // -----------------------------
        // Profile update
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> UpdateUserProfile(UserProfileModel model)
        {
            if (DemoEnabled)
            {
                // Demo: pretend it worked
                return RedirectToAction("Index", "UserProfile");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User is not authenticated.");

            if (_apiBaseUri == null)
                return StatusCode((int)HttpStatusCode.InternalServerError, "ApiBaseUrl missing/invalid.");

            var token = HttpContext.Request.Cookies["jwtCookie"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized("JWT token is missing.");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // IMPORTANT: build URL from ApiBaseUrl, not hard-coded domain
            var requestUrl = new Uri(_apiBaseUri, "api/UserProfileApi/UpdateUserProfile");

            var updateModel = new
            {
                UserId = userIdClaim.Value,
                Email = model.Email,
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber
            };

            var response = await httpClient.PatchAsync(
                requestUrl,
                new StringContent(JsonConvert.SerializeObject(updateModel), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index", "UserProfile");

            var errorContent = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Failed to update profile: {errorContent}");
            return View(model);
        }

        // -----------------------------
        // DTOs used here
        // -----------------------------
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
        }

        public class ErrorResponse
        {
            public string Message { get; set; }
            public IEnumerable<string> Errors { get; set; }
        }

        // Minimal DTOs (so this controller compiles even if API project types are not referenced cleanly)
        public class LoginDto
        {
            public string UsernameOrEmail { get; set; }
            public string Password { get; set; }
        }

        public class UserDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
