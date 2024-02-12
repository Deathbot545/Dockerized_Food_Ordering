using Microsoft.AspNetCore.Mvc;

namespace Food_Ordering_Web.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Administrator/MainPaige.cshtml");
        }
    }
}
