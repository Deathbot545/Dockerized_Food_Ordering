using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Food_Ordering_Web.DTO;

namespace Food_Ordering_Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderController> _logger;

        public OrderController( IHttpContextAccessor httpContextAccessor,IHttpClientFactory httpClientFactory, ILogger<OrderController> logger)
        {
           
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
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

            return View("~/Views/Menu/Menu.cshtml");
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
            return View("~/Views/Menu/FoodItem.cshtml");
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            }
            return View("~/Views/Menu/CheckOut.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateOrder(IFormCollection form)
        {
            _logger.LogInformation("Processing order update...");

            if (!int.TryParse(form["tableId"], out int tableId))
            {
                _logger.LogError("Invalid or missing tableId in form data.");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            if (!int.TryParse(form["outletId"], out int outletId))
            {
                _logger.LogError("Invalid or missing outletId in form data.");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            var userId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            var items = new List<CartItem>();

            for (int i = 0; form.ContainsKey($"items[{i}].id"); i++)
            {
                if (int.TryParse(form[$"items[{i}].id"], out int itemId) &&
                    int.TryParse(form[$"items[{i}].qty"], out int qty) &&
                    decimal.TryParse(form[$"items[{i}].price"], out decimal price) &&
                    form.TryGetValue($"items[{i}].name", out var name) && !string.IsNullOrWhiteSpace(name) &&
                    form.TryGetValue($"items[{i}].note", out var note))
                {
                    items.Add(new CartItem
                    {
                        Id = itemId,
                        Qty = qty,
                        Name = name,
                        Price = price,
                        Note = note
                    });

                    _logger.LogInformation($"Processing item {i}: ID={itemId}, Qty={qty}, Price={price}, Name={name}, Note={note}");
                }
                else
                {
                    _logger.LogError($"Invalid data for item at index {i}.");
                }
            }

            CartRequest orderData = new CartRequest
            {
                UserId = userId,
                TableId = tableId,
                OutletId = outletId,
                MenuItems = items
            };

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(orderData);
            _logger.LogInformation($"Sending JSON payload to API: {jsonPayload}");

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("https://restosolutionssaas.com/api/OrderApi/AddOrder", orderData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OrderResponse>();
                return Json(new { success = true, orderId = result.OrderId, redirectUrl = Url.Action("OrderConfirmation", "Order", new { orderId = result.OrderId }) });
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError($"API call failed: {errorResponse}");
                return Json(new { success = false, errorMessage = errorResponse });
            }
        }

        public class OrderResponse
        {
            public int OrderId { get; set; } // Assuming orderId is an int
            public string Message { get; set; }
        }

        public IActionResult Orderpaige()
        {
            return View("~/Views/Menu/Order.cshtml");
        }

    }
}
