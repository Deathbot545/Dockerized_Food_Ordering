using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using static Food_Ordering_API.Controllers.AccountApiController;
using System.Net.Http.Headers;
using System.Security.Claims;


namespace Food_Ordering_Web.Controllers
{
    [Route("[controller]")]
    public class StripeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public StripeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            var apiKey = configuration["StripeSettings:ApiKey"];
            StripeConfiguration.ApiKey = apiKey;
            _httpClient = httpClientFactory.CreateClient("UserManagementApiClient");
            _apiBaseUrl = $"{configuration.GetValue<string>("ApiBaseUrl")}api";
        }


        [HttpGet("start-subscription")]
        public ActionResult StartSubscription()
        {
            var domain = "https://restosolutionssaas.com"; // Adjust as per your application's domain

            // Assume user's identifier is retrieved from their authentication cookie or JWT token.
            // For demonstration purposes, the userId needs to be dynamically retrieved based on the authenticated user context,
            // which should be passed to Stripe's SuccessUrl to identify the user once Stripe redirects back to your application.

            // This approach requires the client application to pass the userId or ensure the session can resolve it when Stripe redirects back.
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
            // The userId should be resolved here, either by decoding the JWT from the user's request
            // or by maintaining a mapping of session_id to userId on your server (set when creating the Stripe session).
            // For demonstration, this is left as a manual step:

            // string userId = ResolveUserIdFromSession(session_id);
            // This requires an implementation specific to your authentication strategy.

            // Since direct user management is not done here, call your User Management API to handle subscription success.
            var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/HandleSubscriptionSuccess?sessionId={session_id}");

            if (apiResponse.IsSuccessStatusCode)
            {
                // Assuming the API call to update the user's subscription status is successful
                return RedirectToAction("Index", "Home"); // Adjust as needed based on your application's flow
            }
            else
            {
                // Log the error or handle the failure appropriately
                return RedirectToAction("ErrorPage"); // Adjust as needed
            }
        }

        [HttpGet("RegistrationCancel")]
        public IActionResult RegistrationCancel()
        {
            // Handle cancellation. You might want to redirect to a different page or show a message.
            return View("RegistrationCancelled");
        }
    }

}
