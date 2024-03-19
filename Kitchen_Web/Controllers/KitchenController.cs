using Core.DTO;
using Core.Services.Orderser;
using Core.Services.OutletSer;
using Core.ViewModels;
using Kitchen_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Kitchen_Web.Controllers
{
    public class KitchenController : Controller
    {
        private readonly ILogger<KitchenController> _logger;
        private readonly IOutletService _outletService;
  

        public KitchenController(ILogger<KitchenController> logger, IOutletService outletService)
        {
            _logger = logger;
            _outletService = outletService;
           
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
                    return View("~/Views/Home/Index.cshtml", model); // Simplify the view path
                }
            }
            else
            {
                // If no outletId is provided, just return the default view without additional data.
                return View("~/Views/Home/Index.cshtml");
            }

        }

    }
}
