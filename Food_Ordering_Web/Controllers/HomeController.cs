using Food_Ordering_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Food_Ordering_Web.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            
        }
        public IActionResult TestAuth()
        {
            if (User.Identity.IsAuthenticated)
            {
                // If the user is authenticated, return some success message or information
                var userName = User.Identity.Name;
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                // Logging for demonstration
                _logger.LogInformation($"User is authenticated. Name: {userName}, Role: {role}");

                return Content($"User is authenticated. Name: {userName}, Role: {role}");
            }
            else
            {
                // If not authenticated, return an appropriate message
                _logger.LogWarning("User is not authenticated.");
                return Content("User is not authenticated.");
            }
        }

        public IActionResult Index()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    return RedirectToRoleBasedView(userRole);
                }
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request in {ControllerName}", nameof(HomeController));
                Console.WriteLine($"An error occurred: {ex.Message}");
                return View("Error"); 
            }

        }

        private IActionResult RedirectToRoleBasedView(string userRole)
        {
            if (string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", userRole);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // Define a DTO to map API response
        public class UserStatusDTO
        {
            public bool IsSignedIn { get; set; }
            public List<string> Roles { get; set; }
        }

        public IActionResult Regiser_Bussiness() 
        { 
            return View("Views/Account/Regiser_Bussiness.cshtml"); 
        }
        public IActionResult AddStaff()
        {
            return View("Views/Kitchen/AddStaff.cshtml");
        }
    }
}