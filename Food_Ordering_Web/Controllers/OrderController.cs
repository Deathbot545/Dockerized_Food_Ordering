using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Food_Ordering_Web.DTO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Food_Ordering_Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderController> _logger;
        private readonly IConfiguration _configuration;

        private bool DemoEnabled => _configuration.GetValue<bool>("Demo:Enabled");

        // Example: "https://restosolutionssaas.com/"
        private readonly Uri? _apiBaseUri;

        public OrderController(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            ILogger<OrderController> logger,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;

            var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl");
            if (!string.IsNullOrWhiteSpace(apiBaseUrl) &&
                Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var root))
            {
                _apiBaseUri = root;
            }

            _logger.LogInformation("OrderController DemoEnabled: {DemoEnabled}", DemoEnabled);
            _logger.LogInformation("OrderController ApiBaseUrl: {ApiBaseUrl}", apiBaseUrl ?? "(null)");
        }

        public IActionResult Index() => View();

        // -----------------------------
        // MENU
        // -----------------------------
        [HttpGet]
        public IActionResult Menu(int outletId, int tableId)
        {
            ViewBag.OutletId = outletId;
            ViewBag.TableId = tableId;
            ViewBag.DemoEnabled = DemoEnabled;

            TempData["tableId"] = tableId;
            TempData["outletId"] = outletId;

            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.UserId = User.Identity.Name;
            }

            return View("~/Views/Menu/Menu.cshtml");
        }

        // -----------------------------
        // DEMO endpoints used by Menu.cshtml JS (ONLY for demo mode)
        // -----------------------------
        [HttpGet]
        public IActionResult DemoOutletInfo(int outletId)
        {
            if (!DemoEnabled) return NotFound();

            var outlets = GetDemoOutlets();
            var outlet = outlets.FirstOrDefault(x => x.OutletId == outletId) ?? outlets.First();

            return Json(new
            {
                customerFacingName = outlet.CustomerFacingName,
                // JS expects base64 string (not data:image/... prefix)
                logo = outlet.LogoBase64
            });
        }

        [HttpGet]
        public IActionResult DemoMenuItems(int outletId)
        {
            if (!DemoEnabled) return NotFound();

            var items = GetDemoMenuItems(outletId);
            return Json(items);
        }

        [HttpPost]
        public IActionResult DemoCallWaiter([FromBody] JObject payload)
        {
            if (!DemoEnabled) return NotFound();

            var tableId = payload?["tableId"]?.Value<int>() ?? 0;
            _logger.LogInformation("DEMO CallWaiter received for tableId={TableId}", tableId);

            return Ok(new { message = "Demo waiter called", tableId });
        }

        // -----------------------------
        // Food detail redirect flow (FIXED)
        // -----------------------------
        [HttpPost]
        public IActionResult RedirectToDetail(int itemId, int outletId, int tableId, string customerFacingName, int categoryId, int? menuId = null)
        {
            TempData["itemId"] = itemId;
            TempData["outletId"] = outletId;
            TempData["tableId"] = tableId;
            TempData["customerFacingName"] = customerFacingName;
            TempData["categoryId"] = categoryId;
            TempData["menuId"] = menuId;

            var url = Url.Action("FoodDetail", new
            {
                itemId,
                outletId,
                tableId,
                customerFacingName,
                categoryId,
                menuId
            });

            return Json(new
            {
                success = true,
                redirectUrl = url,
                itemId,
                outletId,
                tableId,
                customerFacingName,
                categoryId,
                menuId
            });
        }

        [HttpGet]
        public IActionResult FoodDetail(int? itemId, int? outletId, int? tableId, string? customerFacingName, int? categoryId, int? menuId)
        {
            var finalItemId = itemId ?? TempData["itemId"] as int?;
            var finalOutletId = outletId ?? TempData["outletId"] as int?;
            var finalTableId = tableId ?? TempData["tableId"] as int?;
            var finalName = customerFacingName ?? TempData["customerFacingName"]?.ToString();
            var finalCategoryId = categoryId ?? TempData["categoryId"] as int?;
            var finalMenuId = menuId ?? TempData["menuId"] as int?;

            ViewBag.ItemId = finalItemId;
            ViewBag.OutletId = finalOutletId;
            ViewBag.TableId = finalTableId;
            ViewBag.CustomerFacingName = finalName;
            ViewBag.CategoryId = finalCategoryId;
            ViewBag.MenuId = finalMenuId;
            ViewBag.DemoEnabled = DemoEnabled;

            TempData.Keep("tableId");
            TempData.Keep("outletId");

            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.UserId = User.Identity.Name;
            }

            return View("~/Views/Menu/FoodItem.cshtml");
        }


        [HttpGet]
        public IActionResult Checkout()
        {
            ViewBag.DemoEnabled = DemoEnabled;

            if (User.Identity?.IsAuthenticated == true)
                ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View("~/Views/Menu/CheckOut.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateOrder(IFormCollection form)
        {
            _logger.LogInformation("Processing order update...");
            _logger.LogInformation("Form data received: {FormData}",
                form.ToDictionary(x => x.Key, x => x.Value.ToString()));

            if (!int.TryParse(form["tableId"], out int tableId))
                return BadRequest("Invalid or missing tableId.");

            if (!int.TryParse(form["outletId"], out int outletId))
                return BadRequest("Invalid or missing outletId.");

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            var items = new List<CartItem>();

            for (int i = 0; form.ContainsKey($"items[{i}].id"); i++)
            {
                if (int.TryParse(form[$"items[{i}].id"], out int itemId) &&
                    int.TryParse(form[$"items[{i}].qty"], out int qty) &&
                    decimal.TryParse(form[$"items[{i}].price"], out decimal price) &&
                    form.TryGetValue($"items[{i}].name", out var name) && !string.IsNullOrWhiteSpace(name) &&
                    form.TryGetValue($"items[{i}].note", out var note) &&
                    form.TryGetValue($"items[{i}].size", out var size))
                {
                    var extraItems = new List<ExtraItemRequest>();

                    for (int j = 0; form.ContainsKey($"items[{i}].extraItems[{j}].id"); j++)
                    {
                        if (int.TryParse(form[$"items[{i}].extraItems[{j}].id"], out int extraItemId) &&
                            decimal.TryParse(form[$"items[{i}].extraItems[{j}].price"], out decimal extraItemPrice) &&
                            form.TryGetValue($"items[{i}].extraItems[{j}].name", out var extraItemName) &&
                            !string.IsNullOrWhiteSpace(extraItemName))
                        {
                            extraItems.Add(new ExtraItemRequest
                            {
                                Id = extraItemId,
                                Name = extraItemName,
                                Price = extraItemPrice
                            });
                        }
                    }

                    items.Add(new CartItem
                    {
                        Id = itemId,
                        Name = name,
                        Qty = qty,
                        Price = price,
                        Note = note,
                        Size = size,
                        ExtraItems = extraItems
                    });
                }
            }

            // ---------- DEMO MODE ----------
            if (DemoEnabled)
            {
                var demoOrderId = Random.Shared.Next(10000, 99999);

                _logger.LogInformation("DEMO order created. OrderId={OrderId} OutletId={OutletId} TableId={TableId} Items={Count}",
                    demoOrderId, outletId, tableId, items.Count);

                TempData["LastDemoOrderId"] = demoOrderId;

                return Json(new
                {
                    success = true,
                    orderId = demoOrderId.ToString(),
                    redirectUrl = Url.Action("OrderConfirmation", "Order", new { orderId = demoOrderId })
                });
            }

            // ---------- REAL API MODE ----------
            if (_apiBaseUri == null)
            {
                _logger.LogError("CreateOrUpdateOrder: ApiBaseUrl missing/invalid.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "ApiBaseUrl missing/invalid.");
            }

            var orderDataObject = new JObject
            {
                ["UserId"] = userId,
                ["TableId"] = tableId,
                ["OutletId"] = outletId,
                ["MenuItems"] = JArray.FromObject(items)
            };

            var client = _httpClientFactory.CreateClient();

            var apiUrl = new Uri(_apiBaseUri, "api/OrderApi/AddOrder");
            var response = await client.PostAsJsonAsync(apiUrl, orderDataObject);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JObject.Parse(result);
                var orderId = (string)responseObject.orderId;

                return Json(new
                {
                    success = true,
                    orderId,
                    redirectUrl = Url.Action("OrderConfirmation", "Order", new { orderId })
                });
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed: {Error}", errorResponse);
                return Json(new { success = false, errorMessage = errorResponse });
            }
        }

        [HttpGet]
        public IActionResult OrderConfirmation(string orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.DemoEnabled = DemoEnabled;
            return View("~/Views/Menu/Order.cshtml");
        }

        public IActionResult Orderpaige()
        {
            ViewBag.DemoEnabled = DemoEnabled;
            return View("~/Views/Menu/Order.cshtml");
        }

        // -----------------------------
        // DEMO DATA (menu + outlets)
        // -----------------------------
        private sealed class DemoOutlet
        {
            public int OutletId { get; set; }
            public string CustomerFacingName { get; set; } = "";
            public string LogoBase64 { get; set; } = "";
        }

        private sealed class DemoMenuItemDto
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("price")]
            public decimal Price { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; } = "";
            [JsonPropertyName("image")]
            public string Image { get; set; } = "";
            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; } = "";

            [JsonPropertyName("menuCategoryId")]
            public int MenuCategoryId { get; set; }

            [JsonPropertyName("categoryName")]
            public string CategoryName { get; set; } = "";
        }

        private List<DemoOutlet> GetDemoOutlets()
        {
            const string pixel =
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+XvJwAAAAASUVORK5CYII=";

            return new List<DemoOutlet>
            {
                new DemoOutlet { OutletId = 101, CustomerFacingName = "Scan2Serve Central", LogoBase64 = pixel },
                new DemoOutlet { OutletId = 102, CustomerFacingName = "Kandy Spice House", LogoBase64 = pixel },
                new DemoOutlet { OutletId = 103, CustomerFacingName = "Galle Seaside Grill", LogoBase64 = pixel },
                new DemoOutlet { OutletId = 104, CustomerFacingName = "Negombo Pizza Lab", LogoBase64 = pixel },
                new DemoOutlet { OutletId = 106, CustomerFacingName = "Ella Café & Roastery", LogoBase64 = pixel }
            };
        }

        private List<DemoMenuItemDto> GetDemoMenuItems(int outletId)
        {
            const int starters = 1;
            const int mains = 2;
            const int drinks = 3;
            const int desserts = 4;

            int baseId = outletId * 1000;

            // Deterministic images per item
            static string Img(int id) => $"https://picsum.photos/seed/menu-{id}/1200/800";

            return new List<DemoMenuItemDto>
            {
                new DemoMenuItemDto { Id = baseId + 1,  Name = "Garlic Bread",         Price = 650.00m,  Description = "Toasted baguette, garlic butter, herbs.", Image = "", ImageUrl = Img(baseId + 1),  MenuCategoryId = starters, CategoryName = "Starters" },
                new DemoMenuItemDto { Id = baseId + 2,  Name = "Chicken Wings",        Price = 1250.00m, Description = "Spicy glazed wings with ranch dip.",      Image = "", ImageUrl = Img(baseId + 2),  MenuCategoryId = starters, CategoryName = "Starters" },

                new DemoMenuItemDto { Id = baseId + 10, Name = "Signature Fried Rice", Price = 1450.00m, Description = "Chicken, egg, veg, house soy sauce.",     Image = "", ImageUrl = Img(baseId + 10), MenuCategoryId = mains,    CategoryName = "Mains" },
                new DemoMenuItemDto { Id = baseId + 11, Name = "Seafood Kottu",        Price = 1750.00m, Description = "Classic kottu with prawns & calamari.",    Image = "", ImageUrl = Img(baseId + 11), MenuCategoryId = mains,    CategoryName = "Mains" },
                new DemoMenuItemDto { Id = baseId + 12, Name = "Pepperoni Pizza",      Price = 2200.00m, Description = "Cheese, pepperoni, oregano, thin crust.",  Image = "", ImageUrl = Img(baseId + 12), MenuCategoryId = mains,    CategoryName = "Mains" },

                new DemoMenuItemDto { Id = baseId + 20, Name = "Iced Latte",           Price = 850.00m,  Description = "Chilled espresso + milk, lightly sweet.", Image = "", ImageUrl = Img(baseId + 20), MenuCategoryId = drinks,   CategoryName = "Drinks" },
                new DemoMenuItemDto { Id = baseId + 21, Name = "Fresh Lime Soda",      Price = 550.00m,  Description = "Sparkling lime, mint, and ice.",          Image = "", ImageUrl = Img(baseId + 21), MenuCategoryId = drinks,   CategoryName = "Drinks" },

                new DemoMenuItemDto { Id = baseId + 30, Name = "Chocolate Brownie",    Price = 900.00m,  Description = "Warm brownie with vanilla ice cream.",     Image = "", ImageUrl = Img(baseId + 30), MenuCategoryId = desserts, CategoryName = "Desserts" },
                new DemoMenuItemDto { Id = baseId + 31, Name = "Watalappan",           Price = 750.00m,  Description = "Sri Lankan jaggery custard dessert.",      Image = "", ImageUrl = Img(baseId + 31), MenuCategoryId = desserts, CategoryName = "Desserts" }
            };
        }
        public class OrderResponse
        {
            public int OrderId { get; set; }
            public string Message { get; set; } = "";
        }
    }
}
