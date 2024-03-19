using Core.DTO;
using Core.Services.MenuS;
using Core.Services.OutletSer;
using Core.ViewModels;
using Infrastructure.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class KitchenApiController : Controller
    {
         private readonly IOutletService _outletService;
        private readonly ILogger<KitchenApiController> _logger;

       public KitchenApiController(IOutletService outletService,ILogger<KitchenApiController> logger)
       {
                _outletService = outletService;
            _logger = logger;
       }
         public IActionResult Index()
        {
            return View();
        }
        [HttpPost("AddKitchenStaff")]
        public async Task<IActionResult> AddKitchenStaff([FromForm] KitchenStaffViewModel model)
        {
            _logger.LogInformation($"Adding Kitchen Staff: {JsonConvert.SerializeObject(model)}"); // Log the incoming model

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Model state is invalid: {JsonConvert.SerializeObject(ModelState)}");
                return BadRequest(ModelState);
            }

            try
            {
                bool result = await _outletService.AddKitchenStaffAsync(model);
                if (result)
                {
                    return Ok(new { message = "Staff member added successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to add the staff member" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding kitchen staff: {ex.Message}", ex);
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }


        [HttpGet("GetStaffByOutlet/{outletId}")]
        public async Task<IActionResult> GetStaffByOutlet(int outletId)
        {
            try
            {
                var staffMembers = await _outletService.GetKitchenStaffByOutletAsync(outletId);
                if (staffMembers == null || !staffMembers.Any())
                {
                    return NotFound(new { message = "No staff members found for this outlet." });
                }
                return Ok(staffMembers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the staff members.", details = ex.Message });
            }
        }

        [HttpDelete("DeleteStaffMember/{id}")]
        public async Task<IActionResult> DeleteStaffMember(int id)
        {
            try
            {
                var result = await _outletService.DeleteKitchenStaffAsync(id);
                if (result)
                {
                    return Ok(new { message = "Staff member deleted successfully" });
                }
                else
                {
                    return NotFound(new { message = "Staff member not found" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        [HttpPost("UpdateStaffMember")]
        public async Task<IActionResult> UpdateStaffMember([FromBody] KitchenStaffUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _outletService.UpdateKitchenStaffAsync(model);
                if (result)
                {
                    return Ok(new { message = "Staff member updated successfully" });
                }
                else
                {
                    return NotFound(new { message = "Staff member not found" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }


    }
}
