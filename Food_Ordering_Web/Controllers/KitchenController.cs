using Food_Ordering_Web.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Food_Ordering_Web.Controllers
{
    public class KitchenController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<KitchenController> _logger;
        private readonly bool _demoMode;

        public KitchenController(IConfiguration config, ILogger<KitchenController> logger)
        {
            _config = config;
            _logger = logger;
            _demoMode = _config.GetValue<bool>("DemoMode");
        }

        [HttpGet]
        public IActionResult Index(int outletId, string internalOutletName = "", string customerFacingName = "")
        {
            ViewBag.OutletId = outletId;
            ViewBag.InternalOutletName = internalOutletName;
            ViewBag.CustomerFacingName = customerFacingName;
            ViewBag.DemoMode = _demoMode;

            return View("~/Views/Kitchen/kitchenView.cshtml");
        }

        [HttpGet]
        public IActionResult AddStaff()
        {
            return RedirectToAction("AddStaff", "Restaurant");
        }
    }
}
