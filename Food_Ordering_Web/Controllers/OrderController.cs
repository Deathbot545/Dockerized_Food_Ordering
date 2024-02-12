using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Food_Ordering_Web.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Menu(int outletId, int tableId)
        {
            ViewBag.OutletId = outletId;
            ViewBag.TableId = tableId;

            TempData["tableId"] = tableId;
            TempData["outletId"] = outletId;

            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserId = User.Identity.Name;
            }

            var viewPath = User.Identity.IsAuthenticated
                ? "~/Views/Menu/AuthenticatedMenu.cshtml"
                : "~/Views/Menu/GuestMenu.cshtml";

            return View(viewPath);
        }


        [HttpPost]
        public IActionResult RedirectToDetail(int itemId, int outletId, int tableId, string customerFacingName)
        {
            TempData["itemId"] = itemId;
            TempData["outletId"] = outletId;
            TempData["tableId"] = tableId;
            TempData["customerFacingName"] = customerFacingName;

            TempData.Keep("tableId");
            TempData.Keep("outletId");

            return Json(new { success = true, redirectUrl = Url.Action("FoodDetail") });
        }

        public IActionResult FoodDetail()
        {
            ViewBag.ItemId = TempData["itemId"];
            ViewBag.OutletId = TempData["outletId"];
            ViewBag.TableId = TempData["tableId"];
            ViewBag.CustomerFacingName = TempData["customerFacingName"];

            TempData.Keep("tableId");
            TempData.Keep("outletId");
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserId = User.Identity.Name;
            }


            var viewPath = User.Identity.IsAuthenticated
                ? "~/Views/Menu/AuthFoodItem.cshtml"
                : "~/Views/Menu/FoodItemGuest.cshtml";

            return View(viewPath);
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            }

            ViewBag.TableId = TempData["tableId"];
            ViewBag.OutletId = TempData["outletId"];

            var viewPath = User.Identity.IsAuthenticated
                ? "~/Views/Menu/AuthCheckOut.cshtml"
                : "~/Views/Menu/CheckOutGuest.cshtml";

            return View(viewPath);
        }


    }
}
