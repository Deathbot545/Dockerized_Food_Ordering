using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Text;
using ZXing.QrCode.Internal;

namespace Food_Ordering_Web.Controllers
{
    [Authorize(Roles = "Restaurant")]
    public class RestaurantController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RestaurantController> _logger;

        public RestaurantController(IHttpClientFactory clientFactory, IConfiguration configuration, IWebHostEnvironment environment, ILogger<RestaurantController> logger)
        {
            _httpClient = new HttpClient();
            _apiBaseUrl = configuration.GetValue<string>("RestaurantApiBaseUrl");
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);  // Set the BaseAddress here
            _configuration = configuration;
            _environment = environment;
            _logger = logger;

            _logger.LogInformation($"_apiBaseUrl: {_apiBaseUrl}");
            _logger.LogInformation($"HttpClient Base Address: {_httpClient.BaseAddress}");
        }

        public async Task<IActionResult> Index()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                // Redirect to login or show error
                return RedirectToAction("Login", "Account");
            }

            Guid ownerId = new Guid(currentUserId);

            // You can now use _httpClient instead of creating a new HttpClient
            var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId}");

            if (response.IsSuccessStatusCode)
            {
                var outlets = JsonConvert.DeserializeObject<List<Outlet>>(await response.Content.ReadAsStringAsync());
                return View("~/Views/Owner/MainPaige.cshtml", outlets);
            }

            return View("Error"); // Or handle errors differently
        }
        public IActionResult Add()
        {
            return View("~/Views/Owner/AddOutlet.cshtml");
        }

        public IActionResult Manage(int id, string internalOutletName)
        {
            ViewBag.OutletId = id;
            ViewBag.InternalOutletName = internalOutletName;
            return View("~/Views/Owner/Manage.cshtml");
        }
        public IActionResult Tables(int id, string customerFacingName, string internalOutletName)
        {
            ViewBag.OutletId = id;
            ViewBag.CustomerFacingName = customerFacingName;
            ViewBag.InternalOutletName = internalOutletName;
            return View("~/Views/Owner/Tables.cshtml");
        }

        private List<Table> FetchTablesByOutletId(int id)
        {
            // Fetch tables based on outlet ID and return
            return new List<Table>(); // Replace with your actual logic
        }

        public async Task<IActionResult> AddOutlet([FromForm] Outlet outlet, [FromForm] IFormFile Logo, [FromForm] IFormFile RestaurantImage)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "User is not authenticated" });
            }

            outlet.OwnerId = Guid.Parse(currentUserId);

            if (Logo != null && Logo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Logo.CopyToAsync(ms);
                    outlet.Logo = ms.ToArray();
                }
            }

            if (RestaurantImage != null && RestaurantImage.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await RestaurantImage.CopyToAsync(ms);
                    outlet.RestaurantImage = ms.ToArray();
                }
            }

            // Generate the subdomain name in the controller
            outlet.Subdomain = GenerateSubdomain(outlet.InternalOutletName);

            // Use _httpClient that has BaseAddress set
            var content = new StringContent(JsonConvert.SerializeObject(outlet), Encoding.UTF8, "application/json");
            var apiEndpoint = $"api/OutletApi/RegisterOutlet?currentUserId={currentUserId}";

            var fullApiEndpoint = $"{_httpClient.BaseAddress}{apiEndpoint}";
            var response = await _httpClient.PostAsync(fullApiEndpoint, content);


            if (response.StatusCode == HttpStatusCode.OK)
            {
                string conten = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiOutletResponse>(conten);

                if (apiResponse.Success)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "API indicated failure but returned 200 OK" });
                }
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                string c = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Bad Request: {c}");
                return Json(new { success = false });
            }
            else
            {
                return Json(new { success = false });
            }

        }
        public class ApiOutletResponse
        {
            public bool Success { get; set; }
            public Outlet Outlet { get; set; }
        }
        private string GenerateSubdomain(string internalOutletName)
        {
            // This method will generate a sanitized subdomain based on the InternalOutletName.
            // For now, this just converts the name to lowercase and removes spaces. 
            // Depending on your needs, you might add more complex sanitizing here.
            return internalOutletName.ToLower().Replace(" ", "");
        }

    }
}
