using Microsoft.AspNetCore.Mvc;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RPingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is up and running.");
        }
    }
}
