using Microsoft.AspNetCore.Mvc;

namespace Food_Ordering_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "API Home endpoint." });
        }
    }
}
