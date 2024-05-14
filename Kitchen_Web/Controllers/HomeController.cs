
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
            return View("");
        }

    }
}