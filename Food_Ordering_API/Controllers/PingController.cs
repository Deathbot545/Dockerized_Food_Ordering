using Microsoft.AspNetCore.Mvc;

namespace Food_Ordering_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is up and running.");
        }
    }
}
