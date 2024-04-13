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
            try
            {
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/HandleSubscriptionSuccess?sessionId={session_id}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    _logger.LogError("Failed to fetch outlets. Status Code: {StatusCode}, Content: {Content}", apiResponse.StatusCode, await apiResponse.Content.ReadAsStringAsync());
                    return RedirectToAction("ErrorPage");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred in RegistrationSuccess: {Exception}", ex);
                return RedirectToAction("ErrorPage");
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
