using Core.DTO;
using Core.Services.Orderser;
using Core.Services.OutletSer;
using Core.ViewModels;
using Food_Ordering_Web.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Food_Ordering_Web.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IOutletService _outletService;
        private readonly IOrderService _orderService; // 

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory,IOutletService outletService,IOrderService orderService)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("API");
            _outletService = outletService;
            _orderService = orderService;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                return RedirectToRoleBasedView(userRole);
            }
            return View();
        }

        private IActionResult RedirectToRoleBasedView(string userRole)
        {
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", userRole);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // Define a DTO to map API response
        public class UserStatusDTO
        {
            public bool IsSignedIn { get; set; }
            public List<string> Roles { get; set; }
        }


        public async Task<IActionResult> SubdomainRedirection(string subdomain)
        {
            var outlet = await _outletService.GetOutletBySubdomain(subdomain);
            if (outlet == null) return View("Error", new ErrorViewModel { Message = "Subdomain not found" });

            var tables = _outletService.GetTablesByOutlet(outlet.Id);

            List<OrderDTO> orders;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"https://localhost:7268/api/OrderApi/GetOrdersForOutlet/{outlet.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return View("Error", new ErrorViewModel { Message = $"API Error: {errorResponse}" });
                }

                try
                {
                    // Directly deserialize the response content to List<OrderDTO>
                    orders = await response.Content.ReadAsAsync<List<OrderDTO>>();
                }
                catch (Exception ex)  // catch generic exception for unexpected issues
                {
                    _logger.LogError($"Error while processing the API response: {ex.Message}");
                    return View("Error", new ErrorViewModel { Message = $"API Processing Error: {ex.Message}" });
                }
            }

            var model = new OutletViewModel { OutletInfo = outlet, Tables = tables, Orders = orders };
            return View("Views/Kitchen/kitchenView.cshtml", model);
        }





    }
}