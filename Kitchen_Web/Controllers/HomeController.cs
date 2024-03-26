
using Kitchen_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Order_API.DTO;
using Order_API.Service.Orderser;
using Restaurant_API.Services.OutletSer;
using Restaurant_API.ViewModels;
using System.Diagnostics;

namespace Kitchen_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IOutletService _outletService;
        private readonly IOrderService _orderService; // 

        public HomeController(ILogger<HomeController> logger, IOutletService outletService, IOrderService orderService)
        {
            _logger = logger;
            _outletService = outletService;
            _orderService = orderService;
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

        public async Task<IActionResult> Index(int? outletId)
        {
            if (outletId.HasValue)
            {
                var tables = _outletService.GetTablesByOutlet(outletId.Value);
                List<OrderDTO> orders;
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync($"https://restosolutionssaas.com:7268/api/OrderApi/GetOrdersForOutlet/{outletId.Value}");
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        return View("Error", new ErrorViewModel { Message = $"API Error: {errorResponse}" });
                    }
                    try
                    {
                        orders = await response.Content.ReadAsAsync<List<OrderDTO>>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error while processing the API response: {ex.Message}");
                        return View("Error", new ErrorViewModel { Message = $"API Processing Error: {ex.Message}" });
                    }
                    var model = new OutletViewModel { Tables = tables, Orders = orders };
                    return View("Index", model); // Simplify the view path
                }
            }
            else
            {
                // If no outletId is provided, just return the default view without additional data.
                return View();
            }
        }

    }
}