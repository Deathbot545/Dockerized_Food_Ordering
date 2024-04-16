using Microsoft.AspNetCore.Mvc;

namespace Kitchen_Web.Controllers
{

    [Route("kitchen/[controller]")]
    public class KitPingController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is up and running.");
        }
    }
}
