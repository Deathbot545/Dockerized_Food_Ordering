using Microsoft.AspNetCore.Mvc;

namespace Order_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OPingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is up and running.");
        }
    }
}
