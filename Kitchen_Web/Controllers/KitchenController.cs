
using Kitchen_Web.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Order_API.DTO;
using Restaurant_API.ViewModels;
using System.Diagnostics;

namespace Kitchen_Web.Controllers
{
    [Route("kitchen/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class KitchenController : Controller
    {
        private readonly ILogger<KitchenController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public KitchenController(ILogger<KitchenController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(int? outletId)
        {
            if (!outletId.HasValue)
            {
                // If no outletId is provided, just return the default view without additional data.
                return View("~/Views/Home/Index.cshtml");
            }

            var httpClient = _httpClientFactory.CreateClient();
            List<OrderDTO> orders = new List<OrderDTO>();

            // Fetching Orders
            var response = await httpClient.GetAsync($"https://restosolutionssaas.com/api/OrderApi/GetOrdersForOutlet/{outletId.Value}");
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError($"API Error fetching orders: {errorResponse}");
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

            // Constructing ViewModel to pass to the view. Since only order details are needed and they include TableId,
            // there's no need to fetch or include table information separately.
            var model = new OutletViewModel
            {
                Orders = orders
            };

            return View("~/Views/Home/Index.cshtml", model);
        }
    }
}
