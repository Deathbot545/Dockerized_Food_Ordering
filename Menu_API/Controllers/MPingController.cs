using Microsoft.AspNetCore.Mvc;

namespace Menu_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MPingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is up and running.");
        }
    }
}
