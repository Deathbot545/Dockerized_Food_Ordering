﻿using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Food_Ordering_Web.DTO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
        public IActionResult RedirectToDetail(int itemId, int outletId, int tableId, string customerFacingName, int categoryId)
        {
            return Json(new { success = true, redirectUrl = Url.Action("FoodDetail"), itemId, outletId, tableId, customerFacingName, categoryId });
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

            // Log the entire form data
            _logger.LogInformation("Form data received: {FormData}", form.ToDictionary(x => x.Key, x => x.Value.ToString()));

            // Log the request body as JSON
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string requestBodyJson = await reader.ReadToEndAsync();
                _logger.LogInformation($"Received JSON payload: {requestBodyJson}");
            }

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
                    form.TryGetValue($"items[{i}].note", out var note) &&
                    form.TryGetValue($"items[{i}].size", out var size))
                {
                    var extraItems = new List<ExtraItemRequest>();
                    for (int j = 0; form.ContainsKey($"items[{i}].extraItems[{j}].id"); j++)
                    {
                        if (int.TryParse(form[$"items[{i}].extraItems[{j}].id"], out int extraItemId) &&
                            decimal.TryParse(form[$"items[{i}].extraItems[{j}].price"], out decimal extraItemPrice) &&
                            form.TryGetValue($"items[{i}].extraItems[{j}].name", out var extraItemName) && !string.IsNullOrWhiteSpace(extraItemName))
                        {
                            extraItems.Add(new ExtraItemRequest
                            {
                                Id = extraItemId,
                                Name = extraItemName,
                                Price = extraItemPrice
                            });

                            _logger.LogInformation($"Processing extra item {j} for item {i}: ID={extraItemId}, Name={extraItemName}, Price={extraItemPrice}");
                        }
                        else
                        {
                            _logger.LogError($"Invalid data for extra item at index {j} for item {i}.");
                        }
                    }

                    items.Add(new CartItem
                    {
                        Id = itemId,
                        Name = name, // Include Name field
                        Qty = qty,
                        Price = price,
                        Note = note,
                        Size = size,
                        ExtraItems = extraItems
                    });

                    _logger.LogInformation($"Processing item {i}: ID={itemId}, Qty={qty}, Price={price}, Name={name}, Note={note}, Size={size}, ExtraItems={JsonConvert.SerializeObject(extraItems)}");
                }
                else
                {
                    _logger.LogError($"Invalid data for item at index {i}.");
                }
            }

            var orderDataObject = new JObject
            {
                ["UserId"] = userId,
                ["TableId"] = tableId,
                ["OutletId"] = outletId,
                ["MenuItems"] = JArray.FromObject(items)
            };

            var jsonPayload = orderDataObject.ToString();
            _logger.LogInformation($"Sending JSON payload to API: {jsonPayload}");

            var client = _httpClientFactory.CreateClient();
            _logger.LogInformation($"Sending request to API: {jsonPayload}");

            var response = await client.PostAsJsonAsync("https://restosolutionssaas.com/api/OrderApi/AddOrder", orderDataObject);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JObject.Parse(result);
                var orderId = (string)responseObject.orderId;

                return Json(new { success = true, orderId, redirectUrl = Url.Action("OrderConfirmation", "Order", new { orderId }) });
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
