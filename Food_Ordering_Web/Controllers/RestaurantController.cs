
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

        public RestaurantController(IHttpClientFactory clientFactory, IConfiguration configuration, IWebHostEnvironment environment, ILogger<RestaurantController> logger)
        {
            _httpClient = clientFactory.CreateClient();
            _apiBaseUrl = configuration.GetValue<string>("RestaurantApiBaseUrl");
            _httpClient.BaseAddress = new Uri(_apiBaseUrl); // Ensure this is the correct API base URL
            _configuration = configuration;
            _environment = environment;
            _logger = logger;

            // Logging for debugging purposes
            _logger.LogInformation($"_apiBaseUrl: {_apiBaseUrl}");
            _logger.LogInformation($"HttpClient Base Address: {_httpClient.BaseAddress}");
        }

        public async Task<IActionResult> Index()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid ownerId = new Guid(currentUserId);

            var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId}");
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var outlets = JsonConvert.DeserializeObject<List<OutletInfoDTO>>(responseString) ?? new List<OutletInfoDTO>(); // Ensuring a non-null list
                return View("~/Views/Owner/MainPaige.cshtml", outlets);
            }
            else
            {
                _logger.LogError("Failed to fetch outlets. Status Code: {StatusCode}", response.StatusCode);
                return View("Error");
            }
        }


        public IActionResult Add()
        {
            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;

            if (isSubscribed != null && bool.Parse(isSubscribed))
            {
                return View("~/Views/Owner/AddOutlet.cshtml");
            }
            else
            {
                TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
                return RedirectToAction("Index", "Restaurant"); // Redirect to the action that shows the main page.
            }
        }


        public IActionResult Edit()
        {
            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;

            if (isSubscribed != null && bool.Parse(isSubscribed))
            {
                return View("~/Views/Owner/EditOutlet.cshtml");
            }
            else
            {
                TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
                return RedirectToAction("Index", "Restaurant"); // Redirect to the action that shows the main page.
            }
        }
        public IActionResult Manage(int id, string internalOutletName)
        {
            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;

            if (isSubscribed != null && bool.Parse(isSubscribed))
            {
                ViewBag.OutletId = id;
                ViewBag.InternalOutletName = internalOutletName;
                return View("~/Views/Owner/Manage.cshtml");
            }
            else
            {
                TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
                return RedirectToAction("Index", "Restaurant"); // Redirect to the action that shows the main page.
            }
        }
        public IActionResult Tables(int id, string customerFacingName, string internalOutletName)
        {
            var isSubscribed = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "IsSubscribed")?.Value;

            if (isSubscribed != null && bool.Parse(isSubscribed))
            {
                ViewBag.OutletId = id;
            ViewBag.CustomerFacingName = customerFacingName;
            ViewBag.InternalOutletName = internalOutletName;
            return View("~/Views/Owner/Tables.cshtml");
            }
            else
            {
                TempData["SubscriptionMessage"] = "You need to be subscribed to access this feature.";
                return RedirectToAction("Index", "Restaurant"); // Redirect to the action that shows the main page.
            }
        }

      /*  private List<Table> FetchTablesByOutletId(int id)
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
        public async Task<IActionResult> AddStaff()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            Guid ownerId = new Guid(currentUserId);
            // You might want to fetch the outlets here to let the user select one if needed
            var response = await _httpClient.GetAsync($"api/OutletApi/GetOutletsByOwner/{ownerId}");

            if (response.IsSuccessStatusCode)
            {
                var outlets = JsonConvert.DeserializeObject<List<Outlet>>(await response.Content.ReadAsStringAsync());
                // You can pass these outlets to the view to let the user select an outlet for the kitchen staff
                return View("Views/Kitchen/AddStaff.cshtml",outlets);
            }

            return View("Error"); // Or handle errors differently
        }*/

    }
}
