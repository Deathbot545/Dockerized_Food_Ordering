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

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var responseOrders = await httpClient.GetAsync($"https://restosolutionssaas.com/api/OrderApi/GetOrdersForOutlet/{outletId.Value}");
                var responseTables = await httpClient.GetAsync($"https://restosolutionssaas.com/api/TableApi/GetTablesForOutlet/{outletId.Value}");

                if (!responseOrders.IsSuccessStatusCode || !responseTables.IsSuccessStatusCode)
                {
                    var errorResponse = await responseOrders.Content.ReadAsStringAsync();
                    _logger.LogError("API Error fetching orders: {ErrorResponse}", errorResponse);
                    return View("Error", new ErrorViewModel { Message = $"API Error: {errorResponse}" });
                }

                var orders = await responseOrders.Content.ReadAsAsync<List<OrderDTO>>();
                var tables = await responseTables.Content.ReadAsAsync<List<TableDTO>>();
                _logger.LogInformation("Orders and Tables successfully fetched and parsed.");

                var model = new OutletViewModel { Orders = orders, Tables = tables };
                return View("~/Views/Home/Index.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orders.");
                return View("Error", new ErrorViewModel { Message = "An error occurred." });
            }
        }


    }
}
