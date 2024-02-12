using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Food_Ordering_Web.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public UserProfileController(IConfiguration configuration)
        {
            _apiBaseUrl = $"{configuration.GetValue<string>("ApiBaseUrl")}api/"; // Only 'api/' since the controller name will vary
            _httpClient = new HttpClient { BaseAddress = new Uri(_apiBaseUrl) }; // Set BaseAddress here
        }

        private void SetAuthorizationHeader()
        {
            var jwtToken = Request.Cookies["jwtCookie"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        }

        public async Task<IActionResult> Index()
        {
            SetAuthorizationHeader();
            var response = await _httpClient.GetAsync("UserProfileApi/GetUserProfile"); // Removed the hardcoded URL
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userProfile = Newtonsoft.Json.JsonConvert.DeserializeObject<UserProfileModel>(content);

                return View("~/Views/Home/Profile.cshtml", userProfile);
            }
            else
            {
                // Log the failure here
                return View("Error");
            }

        }

        [HttpPost]
        public async Task<IActionResult> Update(UserProfileModel model)
        {
            SetAuthorizationHeader();
            var json = JsonSerializer.Serialize(model);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("UserProfileApi", httpContent); // Removed the hardcoded URL

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index"); // Redirect back to profile
            }
            else
            {
                return View("Error"); // Add appropriate error handling
            }
        }
       
        public async Task<IActionResult> EditProfile()
        {
            return View("~/Views/Account/UpdateProfile.cshtml");
        }
    }
}
