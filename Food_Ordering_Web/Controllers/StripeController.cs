using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using static Food_Ordering_API.Controllers.AccountApiController;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.VisualStudio.Services.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Food_Ordering_Web.Controllers
{
    [Route("[controller]")]
    public class StripeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<StripeController> _logger;

        public StripeController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<StripeController> logger)
        {
            var apiKey = configuration["StripeSettings:ApiKey"];
            StripeConfiguration.ApiKey = apiKey;
            _httpClient = httpClientFactory.CreateClient("UserManagementApiClient");
            _apiBaseUrl = $"{configuration.GetValue<string>("ApiBaseUrl")}api";
            _logger = logger;
        }

        [HttpGet("start-subscription")]
        public ActionResult StartSubscription()
        {
            var domain = "https://restosolutionssaas.com"; // Adjust as per your application's domain
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", },
                LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = "price_1OrDlcFU6tKdw4RE5iusepry", // Replace with your actual price ID
                    Quantity = 1,
                },
            },
                Mode = "subscription",
                SuccessUrl = domain + "/Stripe/RegistrationSuccess?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Stripe/RegistrationCancel",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url); // Redirect to Stripe to complete payment
        }

        [HttpGet("RegistrationSuccess")]
        public async Task<IActionResult> RegistrationSuccess(string session_id)
        {
            // Retrieve user ID from the current user's claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User ID is missing in the current session.");
                return RedirectToAction("ErrorPage");
            }

            // Update the IsSubscribed status in the database
            var updateResult = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/UserProfileApi/UpdateSubscriptionStatus", new { userId = userId, isSubscribed = true });
            if (!updateResult.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update user subscription status. Status Code: {StatusCode}", updateResult.StatusCode);
                return RedirectToAction("ErrorPage");
            }

            // Update claims and re-sign in the user
            await UpdateUserSubscription(userId, true);

            return RedirectToAction("Index", "Home");
        }

        private async Task UpdateUserSubscription(string userId, bool isSubscribed)
        {
            // Directly update claims in the current session, assuming the API has already updated the user's subscription status in the database
            var claimsIdentity = User.Identity as ClaimsIdentity;

            // Remove the old IsSubscribed claim and add the updated one
            var existingClaim = claimsIdentity.FindFirst("IsSubscribed");
            if (existingClaim != null)
            {
                claimsIdentity.RemoveClaim(existingClaim);
            }
            claimsIdentity.AddClaim(new Claim("IsSubscribed", isSubscribed.ToString()));

            // Create a new ClaimsPrincipal and re-sign in to update the cookie
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties { IsPersistent = true });

            _logger.LogInformation("Updated subscription status for user ID {UserId} and re-authenticated user.", userId);
        }




        [HttpGet("RegistrationCancel")]
        public IActionResult RegistrationCancel()
        {
            // Handle cancellation. You might want to redirect to a different page or show a message.
            return View("RegistrationCancelled");
        }
    }

}
