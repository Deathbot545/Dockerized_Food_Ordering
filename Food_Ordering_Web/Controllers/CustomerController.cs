using Microsoft.AspNetCore.Mvc;

namespace Food_Ordering_Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ILogger<CustomerController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Accessed Customer Index Page");
            _logger.LogInformation("User IsAuthenticated: {IsAuthenticated}", User.Identity.IsAuthenticated);

            if (User.Identity.IsAuthenticated)
            {
                _logger.LogInformation("Authenticated User: {UserName}", User.Identity.Name);
                // Log additional details as needed
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("Claim {ClaimType}: {ClaimValue}", claim.Type, claim.Value);
                }
            }
            else
            {
                _logger.LogWarning("User is not authenticated.");
            }

            return View("~/Views/Customer/MainPaige.cshtml");
        }
    }

}
