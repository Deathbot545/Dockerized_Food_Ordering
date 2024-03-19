using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using static Food_Ordering_API.Controllers.AccountApiController;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Models;
using System.Security.Claims;

namespace Food_Ordering_Web.Controllers
{
    [Route("[controller]")]
    public class StripeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly string _apiBaseUrl;
        public StripeController(HttpClient httpClient, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            // Initialize Stripe with your secret key
            StripeConfiguration.ApiKey = "sk_test_51NjhBQFU6tKdw4REQ4sdK5t4EUN3aNkvW7Z3v9e41eXjEgfHwcnFztdPvwrRIFeGgwuMpzvkrcn8CSghhoCbJS9S006L3W13JP";
            _httpClient = httpClient;
            _userManager = userManager;
            _signInManager = signInManager;
            _apiBaseUrl = $"{configuration.GetValue<string>("ApiBaseUrl")}api/AccountApi";
        }

        [HttpGet("start-subscription")] // Change to [HttpGet] if you're initiating via a link or button
        public ActionResult StartSubscription()
        {
            var userId = _userManager.GetUserId(User);
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
                SuccessUrl = domain + $"/Stripe/RegistrationSuccess?session_id={{CHECKOUT_SESSION_ID}}&userId={userId}",
                CancelUrl = domain + "/Stripe/RegistrationCancel",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url); // Redirect to Stripe to complete payment
        }

        [HttpGet("RegistrationSuccess")]
        public async Task<IActionResult> RegistrationSuccess(string session_id, string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // Prepare the request to update the subscription status
                var updateModel = new UpdateSubscriptionStatusDto
                {
                    UserId = userId,
                    IsSubscribed = true
                };

                var jsonContent = JsonConvert.SerializeObject(updateModel);
                var buffer = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                var byteContent = new ByteArrayContent(buffer) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } };

                // Call your API to update the subscription status in the database
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/UpdateSubscriptionStatus", byteContent);

                if (response.IsSuccessStatusCode)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        // Retrieve user's claims and update the IsSubscribed claim
                        var claims = await _userManager.GetClaimsAsync(user);
                        var subscriptionClaim = claims.FirstOrDefault(c => c.Type == "IsSubscribed");
                        if (subscriptionClaim != null)
                        {
                            // Remove the old claim
                            await _userManager.RemoveClaimAsync(user, subscriptionClaim);
                        }

                        // Add the new IsSubscribed claim
                        await _userManager.AddClaimAsync(user, new Claim("IsSubscribed", "true"));

                        // Refresh user's authentication cookie to include the updated claim
                        await _signInManager.RefreshSignInAsync(user);

                        // Redirect to the desired page after updating the subscription status and refreshing claims
                        return RedirectToAction("Index", "Restaurant"); // Adjust as needed
                    }
                }

            }

            // Handle error or unsuccessful update
            return RedirectToAction("ErrorPage"); // Adjust as needed
        }




        [HttpGet("RegistrationCancel")]
        public IActionResult RegistrationCancel()
        {
            // Handle cancellation. You might want to redirect to a different page or show a message.
            return View("RegistrationCancelled");
        }
    }
}
