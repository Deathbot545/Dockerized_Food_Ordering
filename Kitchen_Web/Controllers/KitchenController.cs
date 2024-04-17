using Kitchen_Web.DTO;
using Kitchen_Web.Models;
using Kitchen_Web.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace Kitchen_Web.Controllers
{
    [Route("kitchen")]
    public class KitchenController : Controller
    {
        private readonly ILogger<KitchenController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public KitchenController(ILogger<KitchenController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        [HttpGet("{outletId:int}")]
        public async Task<IActionResult> Index(int? outletId)
        {
            _logger.LogInformation("Entering Index method. OutletId: {OutletId}", outletId);

            if (!outletId.HasValue)
            {
                _logger.LogInformation("No outletId provided, returning default view.");
                return View("~/Views/Home/Index.cshtml");
            }

            var httpClient = _httpClientFactory.CreateClient();
            List<OrderDTO> orders = new List<OrderDTO>();

            var response = await httpClient.GetAsync($"https://restosolutionssaas.com/api/OrderApi/GetOrdersForOutlet/{outletId.Value}");
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("API Error fetching orders: {ErrorResponse}", errorResponse);
                return View("Error", new ErrorViewModel { Message = $"API Error: {errorResponse}" });
            }

            try
            {
                orders = await response.Content.ReadAsAsync<List<OrderDTO>>();
                _logger.LogInformation("Orders successfully fetched and parsed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing the API response.");
                return View("Error", new ErrorViewModel { Message = $"API Processing Error: {ex.Message}" });
            }

            var model = new OutletViewModel
            {
                Orders = orders
            };

            return View("~/Views/Home/Index.cshtml", model);
        }
    }
}
