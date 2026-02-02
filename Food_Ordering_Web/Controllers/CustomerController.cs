using Food_Ordering_Web.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Food_Ordering_Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        private bool DemoEnabled => _configuration.GetValue<bool>("Demo:Enabled");

        // Example: "https://restosolutionssaas.com/"
        private readonly Uri? _apiBaseUri;

        public CustomerController(
            ILogger<CustomerController> logger,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _configuration = configuration;

            var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");

            if (!string.IsNullOrWhiteSpace(apiBaseUrl) &&
                Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var root))
            {
                _apiBaseUri = root;
            }

            _logger.LogInformation("CustomerController DemoEnabled: {DemoEnabled}", DemoEnabled);
            _logger.LogInformation("CustomerController ApiBaseUrl: {ApiBaseUrl}", apiBaseUrl ?? "(null)");
        }

        // -----------------------------
        // Customer Home
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ✅ Demo mode: no auth required, no API calls
            if (DemoEnabled)
            {
                return View("~/Views/Customer/MainPaige.cshtml", GetDemoOutlets());
            }

            // ✅ Non-demo: require auth
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                _logger.LogWarning("Customer.Index: user not authenticated.");
                return RedirectToAction("Login", "Account");
            }

            if (_apiBaseUri == null)
            {
                _logger.LogError("Customer.Index: ApiBaseUrl missing/invalid.");
                return View("Error");
            }

            try
            {
                var client = CreateApiClientWithJwt();

                // IMPORTANT: build URL from ApiBaseUrl
                var url = new Uri(_apiBaseUri, "api/OutletApi/GetAllOutletsAsync");
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Customer.Index: failed to fetch outlets. Status={Status} Body={Body}",
                        response.StatusCode, errorBody);

                    return View("Error");
                }

                var json = await response.Content.ReadAsStringAsync();
                var outlets = JsonConvert.DeserializeObject<List<OutletInfoDTO>>(json) ?? new List<OutletInfoDTO>();

                // Optional: If API returns empty list, show demo list so UI looks good
                if (outlets.Count == 0)
                {
                    _logger.LogWarning("Customer.Index: API returned 0 outlets. Using demo outlets for UI.");
                    outlets = GetDemoOutlets();
                }

                return View("~/Views/Customer/MainPaige.cshtml", outlets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Customer.Index: exception while loading outlets. Falling back to demo outlets.");
                return View("~/Views/Customer/MainPaige.cshtml", GetDemoOutlets());
            }
        }

        // -----------------------------
        // Customer Orders
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            // ✅ Demo mode: show demo orders
            if (DemoEnabled)
            {
                return View("~/Views/Customer/MyOrders.cshtml", GetDemoOrders());
            }

            // ✅ Non-demo: require auth
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                _logger.LogWarning("Customer.MyOrders: user not authenticated.");
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                _logger.LogWarning("Customer.MyOrders: NameIdentifier claim missing.");
                return RedirectToAction("Login", "Account");
            }

            if (_apiBaseUri == null)
            {
                _logger.LogError("Customer.MyOrders: ApiBaseUrl missing/invalid.");
                return View("Error");
            }

            try
            {
                var client = CreateApiClientWithJwt();

                var url = new Uri(_apiBaseUri, $"api/OrderApi/GetOrdersByUser/{currentUserId}");
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Customer.MyOrders: failed to fetch orders. Status={Status} Body={Body}",
                        response.StatusCode, errorBody);

                    return View("Error");
                }

                var json = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<List<OrderDTO>>(json) ?? new List<OrderDTO>();

                return View("~/Views/Customer/MyOrders.cshtml", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Customer.MyOrders: exception while loading orders.");
                return View("Error");
            }
        }

        // -----------------------------
        // Helpers
        // -----------------------------
        private HttpClient CreateApiClientWithJwt()
        {
            var client = _clientFactory.CreateClient();

            // Attach JWT from cookie if available (API might require Bearer auth)
            var token = HttpContext.Request.Cookies["jwtCookie"];
            if (!string.IsNullOrWhiteSpace(token) && token != "demo-token")
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        // -----------------------------
        // Demo Data
        // -----------------------------
        private List<OutletInfoDTO> GetDemoOutlets()
        {
            return new List<OutletInfoDTO>
    {
        new OutletInfoDTO
        {
            CustomerFacingName = "Scan2Serve Central",
            Logo = null,
            RestaurantImage = null,
            OperatingHoursStart = new TimeSpan(10, 30, 0),
            OperatingHoursEnd = new TimeSpan(23, 0, 0),
            Contact = "+94 11 234 5678",
            Country = "Sri Lanka",
            City = "Colombo"
        },
        new OutletInfoDTO
        {
            CustomerFacingName = "Galle Seaside Grill",
            Logo = null,
            RestaurantImage = null,
            OperatingHoursStart = new TimeSpan(11, 0, 0),
            OperatingHoursEnd = new TimeSpan(23, 30, 0),
            Contact = "+94 91 223 1100",
            Country = "Sri Lanka",
            City = "Galle"
        },
        new OutletInfoDTO
        {
            CustomerFacingName = "Negombo Pizza Lab",
            Logo = null,
            RestaurantImage = null,
            OperatingHoursStart = new TimeSpan(12, 0, 0),
            OperatingHoursEnd = new TimeSpan(22, 0, 0),
            Contact = "+94 31 222 7788",
            Country = "Sri Lanka",
            City = "Negombo"
        },
        new OutletInfoDTO
        {
            CustomerFacingName = "Ella Café & Roastery",
            Logo = null,
            RestaurantImage = null,
            OperatingHoursStart = new TimeSpan(7, 0, 0),
            OperatingHoursEnd = new TimeSpan(20, 0, 0),
            Contact = "+94 57 222 4411",
            Country = "Sri Lanka",
            City = "Ella"
        }
    };
        }



        private List<OrderDTO> GetDemoOrders()
        {
            // NOTE: adjust fields to match your OrderDTO structure
            return new List<OrderDTO>
            {
                new OrderDTO
                {
                    Id = 9001,
                    TableId = 3,
                    // OrderTime = DateTimeOffset.UtcNow.AddMinutes(-42), // if your DTO supports
                    // Status = OrderStatus.Pending,
                    // OrderDetails = ...
                },
                new OrderDTO
                {
                    Id = 9002,
                    TableId = 7,
                    // Status = OrderStatus.Ready,
                }
            };
        }
    }
}
