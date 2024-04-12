
using Food_Ordering_Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Services.Users;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Food_Ordering_Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly IHttpClientFactory _clientFactory;


        public CustomerController(ILogger<CustomerController> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory; 
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated.");
                return RedirectToAction("Login", "Account");
            }

            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync("https://restosolutionssaas.com/api/OutletApi/GetAllOutletsAsync");

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var outlets = JsonConvert.DeserializeObject<List<OutletInfoDTO>>(responseString) ?? new List<OutletInfoDTO>(); // Ensuring a non-null list
                return View("~/Views/Customer/MainPaige.cshtml", outlets);
            }
            else
            {
                _logger.LogError("Failed to fetch outlets. Status Code: {StatusCode}", response.StatusCode);
                return View("Error");
            }
        }

        public async Task<IActionResult> MyOrders()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                // Redirect to login or show error
                return RedirectToAction("Login", "Account");
            }

            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync($"https://restosolutionssaas.com/api/OrderApi/GetOrdersByUser/{currentUserId}");

            if (response.IsSuccessStatusCode)
            {
                var ordersString = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<List<OrderDTO>>(ordersString); // Adjust OrderDTO based on actual structure
                return View("~/Views/Customer/MyOrders.cshtml",orders); // Assuming you have a view ready to display the orders
            }
            else
            {
                // Log the error or handle it as per your error handling policy
                return View("Error"); // Or redirect to a custom error page
            }
        }
    }

}
