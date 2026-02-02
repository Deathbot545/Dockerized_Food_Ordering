using Food_Ordering_Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Food_Ordering_Web.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiBaseUrl;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RestaurantController> _logger;

        // Demo toggle (set in appsettings.Development.json)
        // "Demo": { "Enabled": true }
        private bool DemoEnabled => _configuration.GetValue<bool>("Demo:Enabled");

        public RestaurantController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<RestaurantController> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;

            _httpClient = clientFactory.CreateClient();

            _apiBaseUrl = configuration.GetValue<string>("RestaurantApiBaseUrl");

            // Only set BaseAddress if config exists and is valid
            if (!string.IsNullOrWhiteSpace(_apiBaseUrl) &&
                Uri.TryCreate(_apiBaseUrl, UriKind.Absolute, out var baseUri))
            {
                _httpClient.BaseAddress = baseUri;
            }

            _logger.LogInformation("DemoEnabled: {DemoEnabled}", DemoEnabled);
            _logger.LogInformation("RestaurantApiBaseUrl: {ApiBaseUrl}", _apiBaseUrl ?? "(null)");
            _logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress?.ToString() ?? "(null)");
        }

        // ----------------------------
        // Pages
        // ----------------------------

        public async Task<IActionResult> Index()
        {
            if (DemoEnabled)
            {
                // Minimal safe dummy data (won't break compilation even if DTO has more fields)
                var outlets = GetDemoOutlets();
                return View("~/Views/Owner/MainPaige.cshtml", outlets);
            }

            // Real mode: requires authentication
            var ownerId = TryGetOwnerId();
            if (ownerId == null)
                return RedirectToAction("Login", "Account");

            if (_httpClient.BaseAddress == null)
            {
                _logger.LogError("RestaurantApiBaseUrl is not configured or invalid.");
                return View("Error");
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId.Value}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch outlets. Status Code: {StatusCode}", response.StatusCode);
                    return View("Error");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var outletsReal = JsonConvert.DeserializeObject<List<OutletDTO>>(responseString) ?? new List<OutletDTO>();

                return View("~/Views/Owner/MainPaige.cshtml", outletsReal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching outlets.");
                return View("Error");
            }
        }

        public IActionResult Add()
        {
            if (DemoEnabled || UserHasSubscription())
                return View("~/Views/Owner/AddOutlet.cshtml");

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        public IActionResult Edit()
        {
            if (DemoEnabled || UserHasSubscription())
                return View("~/Views/Owner/EditOutlet.cshtml");

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        public IActionResult Manage(int id, string internalOutletName)
        {
            if (DemoEnabled || UserHasSubscription())
            {
                ViewBag.OutletId = id;
                ViewBag.InternalOutletName = internalOutletName;
                return View("~/Views/Owner/Manage.cshtml");
            }

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        public IActionResult Tables(int id, string customerFacingName, string internalOutletName)
        {
            if (DemoEnabled || UserHasSubscription())
            {
                ViewBag.OutletId = id;
                ViewBag.CustomerFacingName = customerFacingName;
                ViewBag.InternalOutletName = internalOutletName;

                // If your Tables.cshtml expects a model, you can pass demo tables here.
                // return View("~/Views/Owner/Tables.cshtml", GetDemoTables(id));

                return View("~/Views/Owner/Tables.cshtml");
            }

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        // ----------------------------
        // Actions that call backend
        // ----------------------------

        public async Task<IActionResult> AddOutlet([FromForm] OutletDTO outlet, [FromForm] IFormFile? Logo, [FromForm] IFormFile? RestaurantImage)
        {
            if (DemoEnabled)
            {
                // UI-only mode: pretend it saved successfully
                return Json(new { success = true, demo = true });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Json(new { success = false, message = "User is not authenticated" });

            if (_httpClient.BaseAddress == null)
                return Json(new { success = false, message = "RestaurantApiBaseUrl is missing/invalid" });

            try
            {
                // In real mode you want OwnerId tied to current user
                // If OutletDTO doesn't have OwnerId, remove next line
                outlet.OwnerId = Guid.Parse(currentUserId);

                if (Logo != null && Logo.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await Logo.CopyToAsync(ms);
                    outlet.Logo = ms.ToArray();
                }

                if (RestaurantImage != null && RestaurantImage.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await RestaurantImage.CopyToAsync(ms);
                    outlet.RestaurantImage = ms.ToArray();
                }

                // Generate subdomain
                outlet.Subdomain = GenerateSubdomain(outlet.InternalOutletName);

                var content = new StringContent(JsonConvert.SerializeObject(outlet), Encoding.UTF8, "application/json");

                // IMPORTANT: Since BaseAddress is set, use relative URL here:
                var apiEndpoint = $"api/OutletApi/RegisterOutlet?currentUserId={currentUserId}";
                var response = await _httpClient.PostAsync(apiEndpoint, content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiOutletResponse>(responseBody);

                    if (apiResponse?.Success == true)
                        return Json(new { success = true });

                    return Json(new { success = false, message = "API returned 200 but indicated failure." });
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Bad Request: {Body}", body);
                    return Json(new { success = false, message = body });
                }

                return Json(new { success = false, message = $"Unexpected status: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while adding outlet.");
                return Json(new { success = false, message = "Server error while adding outlet." });
            }
        }

        public async Task<IActionResult> AddStaff()
        {
            if (DemoEnabled)
            {
                var outlets = GetDemoOutlets();
                return View("~/Views/Kitchen/AddStaff.cshtml", outlets);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return RedirectToAction("Login", "Account");

            if (_httpClient.BaseAddress == null)
            {
                _logger.LogError("RestaurantApiBaseUrl is not configured or invalid.");
                return View("Error");
            }

            try
            {
                var ownerId = Guid.Parse(currentUserId);
                var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId}");

                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var outlets = JsonConvert.DeserializeObject<List<OutletDTO>>(await response.Content.ReadAsStringAsync())
                              ?? new List<OutletDTO>();

                return View("~/Views/Kitchen/AddStaff.cshtml", outlets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while loading AddStaff.");
                return View("Error");
            }
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        private Guid? TryGetOwnerId()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return null;

            if (!Guid.TryParse(currentUserId, out var ownerId))
                return null;

            return ownerId;
        }

        private bool UserHasSubscription()
        {
            var isSubscribed = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;
            return !string.IsNullOrWhiteSpace(isSubscribed) &&
                   bool.TryParse(isSubscribed, out var ok) &&
                   ok;
        }

        private string GenerateSubdomain(string internalOutletName)
        {
            if (string.IsNullOrWhiteSpace(internalOutletName))
                return "outlet";

            return internalOutletName.ToLower().Replace(" ", "");
        }

        private List<OutletDTO> GetDemoOutlets()
        {
            // Keep it minimal to avoid compile errors if your DTO differs.
            // If your views expect certain fields, add them here to match your DTO.
            return new List<OutletDTO>
            {
                new OutletDTO(),
                new OutletDTO()
            };
        }

        private List<TableDTO> GetDemoTables(int outletId)
        {
            return new List<TableDTO>
            {
                new TableDTO(),
                new TableDTO()
            };
        }

        public class ApiOutletResponse
        {
            public bool Success { get; set; }
            public OutletDTO? Outlet { get; set; }
        }
    }
}
