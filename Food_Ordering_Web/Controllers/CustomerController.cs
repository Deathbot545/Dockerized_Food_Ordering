using Microsoft.AspNetCore.Mvc;

namespace Food_Ordering_Web.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Customer/MainPaige.cshtml");
        }
    }
}
