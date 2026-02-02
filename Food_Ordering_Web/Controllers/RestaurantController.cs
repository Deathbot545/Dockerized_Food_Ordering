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
        private readonly string _apiBaseUrl;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RestaurantController> _logger;
        private readonly bool _demoMode;

        public RestaurantController(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<RestaurantController> logger)
        {
            _httpClient = clientFactory.CreateClient();
            _configuration = configuration;
            _environment = environment;
            _logger = logger;

            _demoMode = configuration.GetValue<bool>("DemoMode");

            _apiBaseUrl = configuration.GetValue<string>("RestaurantApiBaseUrl") ?? "";
            if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            }

            _logger.LogInformation("DemoMode: {DemoMode}", _demoMode);
            _logger.LogInformation("_apiBaseUrl: {ApiBaseUrl}", _apiBaseUrl);
            _logger.LogInformation("HttpClient Base Address: {BaseAddress}", _httpClient.BaseAddress);
        }

        public async Task<IActionResult> Index()
        {
            // ✅ DEMO: Always show realistic outlets without requiring login / API
            if (_demoMode)
            {
                var demoOutlets = GetDemoOutlets();
                return View("~/Views/Owner/MainPaige.cshtml", demoOutlets);
            }

            // If not demo, try API. If user isn't logged in, still don't crash.
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                _logger.LogWarning("User not authenticated. Falling back to demo outlets.");
                return View("~/Views/Owner/MainPaige.cshtml", GetDemoOutlets());
            }

            if (!Guid.TryParse(currentUserId, out var ownerId))
            {
                _logger.LogWarning("Invalid user id claim. Falling back to demo outlets. Claim={Claim}", currentUserId);
                return View("~/Views/Owner/MainPaige.cshtml", GetDemoOutlets());
            }

            try
            {
                var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var outlets = JsonConvert.DeserializeObject<List<OutletDTO>>(responseString) ?? new List<OutletDTO>();

                    // If API returns empty list, show demo so UI still looks great
                    if (outlets.Count == 0)
                    {
                        _logger.LogWarning("API returned 0 outlets. Showing demo outlets.");
                        outlets = GetDemoOutlets();
                    }

                    return View("~/Views/Owner/MainPaige.cshtml", outlets);
                }

                _logger.LogError("Failed to fetch outlets. Status Code: {StatusCode}", response.StatusCode);
                return View("~/Views/Owner/MainPaige.cshtml", GetDemoOutlets());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching outlets. Falling back to demo outlets.");
                return View("~/Views/Owner/MainPaige.cshtml", GetDemoOutlets());
            }
        }

        // --------------------------
        // Existing actions (kept)
        // --------------------------

        public IActionResult Add()
        {
            if (_demoMode) return View("~/Views/Owner/AddOutlet.cshtml");

            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;
            if (isSubscribed != null && bool.Parse(isSubscribed))
                return View("~/Views/Owner/AddOutlet.cshtml");

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        public IActionResult Edit()
        {
            if (_demoMode) return View("~/Views/Owner/EditOutlet.cshtml");

            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;
            if (isSubscribed != null && bool.Parse(isSubscribed))
                return View("~/Views/Owner/EditOutlet.cshtml");

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        public IActionResult Manage(int id, string internalOutletName)
        {
            if (_demoMode)
            {
                ViewBag.OutletId = id;
                ViewBag.InternalOutletName = internalOutletName;
                return View("~/Views/Owner/Manage.cshtml");
            }

            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;
            if (isSubscribed != null && bool.Parse(isSubscribed))
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
            if (_demoMode)
            {
                ViewBag.OutletId = id;
                ViewBag.CustomerFacingName = customerFacingName;
                ViewBag.InternalOutletName = internalOutletName;
                return View("~/Views/Owner/Tables.cshtml");
            }

            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;
            if (isSubscribed != null && bool.Parse(isSubscribed))
            {
                ViewBag.OutletId = id;
                ViewBag.CustomerFacingName = customerFacingName;
                ViewBag.InternalOutletName = internalOutletName;
                return View("~/Views/Owner/Tables.cshtml");
            }

            TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
            return RedirectToAction("Index", "Restaurant");
        }

        public async Task<IActionResult> AddStaff()
        {
            if (_demoMode)
            {
                // For demo, show empty outlet picker or build a small list:
                var demo = GetDemoOutlets();
                return View("Views/Kitchen/AddStaff.cshtml", demo);
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            Guid ownerId = new Guid(currentUserId);
            var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId}");

            if (response.IsSuccessStatusCode)
            {
                var outlets = JsonConvert.DeserializeObject<List<OutletDTO>>(await response.Content.ReadAsStringAsync());
                return View("Views/Kitchen/AddStaff.cshtml", outlets);
            }

            return View("Error");
        }

        // --------------------------
        // Demo data (realistic)
        // --------------------------
        private List<OutletDTO> GetDemoOutlets()
        {
            var today = DateTime.Today;

            return new List<OutletDTO>
            {
                new OutletDTO
                {
                    Id = 101,
                    InternalOutletName = "restro_central_colombo",
                    CustomerFacingName = "Scan2Serve Central",
                    BusinessType = "Fine Dining",
                    Country = "Sri Lanka",
                    State = "Western Province",
                    City = "Colombo",
                    StreetAddress = "No. 12, Park Street, Colombo 02",
                    Description = "Premium dine-in experience with QR ordering, table service alerts, and live kitchen status. Ideal for evening crowds and corporate reservations.",
                    Zip = "00200",
                    PostalCode = "00200",
                    DateOpened = new DateTime(2019, 6, 12),
                    EmployeeCount = 28,
                    OperatingHoursStart = new TimeSpan(10, 30, 0),
                    OperatingHoursEnd = new TimeSpan(23, 00, 0),
                    Contact = "+94 11 234 5678",
                    CreatedAt = today.AddDays(-120),
                    UpdatedAt = today.AddDays(-3)
                },
                new OutletDTO
                {
                    Id = 102,
                    InternalOutletName = "kandy_spice_house",
                    CustomerFacingName = "Kandy Spice House",
                    BusinessType = "Sri Lankan Cuisine",
                    Country = "Sri Lanka",
                    State = "Central Province",
                    City = "Kandy",
                    StreetAddress = "80, Peradeniya Road, Kandy",
                    Description = "Authentic rice & curry, short eats, and family platters. Optimized for fast checkout and high table turnover.",
                    Zip = "20000",
                    PostalCode = "20000",
                    DateOpened = new DateTime(2021, 2, 5),
                    EmployeeCount = 14,
                    OperatingHoursStart = new TimeSpan(8, 0, 0),
                    OperatingHoursEnd = new TimeSpan(21, 30, 0),
                    Contact = "+94 81 222 3344",
                    CreatedAt = today.AddDays(-260),
                    UpdatedAt = today.AddDays(-7)
                },
                new OutletDTO
                {
                    Id = 103,
                    InternalOutletName = "galle_seaside_grill",
                    CustomerFacingName = "Galle Seaside Grill",
                    BusinessType = "Seafood & Grill",
                    Country = "Sri Lanka",
                    State = "Southern Province",
                    City = "Galle",
                    StreetAddress = "11, Lighthouse Street, Galle Fort",
                    Description = "Seafood specials with kitchen dashboards for grilling stations. Great for tourists — multi-language menu ready.",
                    Zip = "80000",
                    PostalCode = "80000",
                    DateOpened = new DateTime(2018, 11, 20),
                    EmployeeCount = 22,
                    OperatingHoursStart = new TimeSpan(11, 0, 0),
                    OperatingHoursEnd = new TimeSpan(23, 30, 0),
                    Contact = "+94 91 223 1100",
                    CreatedAt = today.AddDays(-520),
                    UpdatedAt = today.AddDays(-1)
                },
                new OutletDTO
                {
                    Id = 104,
                    InternalOutletName = "negombo_pizza_lab",
                    CustomerFacingName = "Negombo Pizza Lab",
                    BusinessType = "Fast Casual",
                    Country = "Sri Lanka",
                    State = "Western Province",
                    City = "Negombo",
                    StreetAddress = "25, Lewis Place, Negombo",
                    Description = "Fast casual with build-your-own pizzas. QR ordering + live queue view for pickup and dine-in.",
                    Zip = "11500",
                    PostalCode = "11500",
                    DateOpened = new DateTime(2022, 8, 14),
                    EmployeeCount = 10,
                    OperatingHoursStart = new TimeSpan(12, 0, 0),
                    OperatingHoursEnd = new TimeSpan(22, 0, 0),
                    Contact = "+94 31 222 7788",
                    CreatedAt = today.AddDays(-180),
                    UpdatedAt = today.AddDays(-9)
                },
                new OutletDTO
                {
                    Id = 105,
                    InternalOutletName = "jaffna_curry_club",
                    CustomerFacingName = "Jaffna Curry Club",
                    BusinessType = "Regional Cuisine",
                    Country = "Sri Lanka",
                    State = "Northern Province",
                    City = "Jaffna",
                    StreetAddress = "4, Hospital Road, Jaffna",
                    Description = "Signature Jaffna flavors with spice-level customization. Kitchen tickets auto-grouped by table and course.",
                    Zip = "40000",
                    PostalCode = "40000",
                    DateOpened = new DateTime(2020, 1, 10),
                    EmployeeCount = 16,
                    OperatingHoursStart = new TimeSpan(9, 30, 0),
                    OperatingHoursEnd = new TimeSpan(21, 0, 0),
                    Contact = "+94 21 222 9900",
                    CreatedAt = today.AddDays(-340),
                    UpdatedAt = today.AddDays(-5)
                },
                new OutletDTO
                {
                    Id = 106,
                    InternalOutletName = "ella_cafe_roastery",
                    CustomerFacingName = "Ella Café & Roastery",
                    BusinessType = "Cafe",
                    Country = "Sri Lanka",
                    State = "Uva Province",
                    City = "Ella",
                    StreetAddress = "Main Street, Ella",
                    Description = "Coffee + brunch spot with peak-hour ordering. Lightweight menu for tourists with quick re-order.",
                    Zip = "90090",
                    PostalCode = "90090",
                    DateOpened = new DateTime(2023, 4, 2),
                    EmployeeCount = 8,
                    OperatingHoursStart = new TimeSpan(7, 0, 0),
                    OperatingHoursEnd = new TimeSpan(20, 0, 0),
                    Contact = "+94 57 222 4411",
                    CreatedAt = today.AddDays(-90),
                    UpdatedAt = today.AddDays(-2)
                }
            };
        }
    }
}
