using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using ZXing.QrCode.Internal;
using Core.Services.OutletSer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;
using Core.DTO;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class OutletApiController : ControllerBase
    {
        private readonly IOutletService _outletService;

        public OutletApiController(IOutletService outletService)
        {
            _outletService = outletService;
        }
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteOutlet(int id)
        {
            try
            {
                var result = await _outletService.DeleteOutletByIdAsync(id);
                if (result)
                {
                    return Ok(new { Message = "Outlet successfully deleted" });
                }
                else
                {
                    return NotFound(new { Message = "Outlet not found" });
                }
            }
            catch (Exception ex)
            {
                // Log the exception, consider using a logging framework
                return BadRequest(new { Message = "An error occurred", Details = ex.Message });
            }
        }

        [HttpPatch("update/{id}"), Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateOutlet(int id)
        {
            try
            {
                var updateDTOJson = Request.Form["updateDTO"];
                if (string.IsNullOrWhiteSpace(updateDTOJson))
                {
                    return BadRequest("Invalid update data. The 'updateDTO' form field is required.");
                }

                var updateDTO = JsonConvert.DeserializeObject<OutletUpdateDTO>(updateDTOJson);
                if (updateDTO == null)
                {
                    return BadRequest("Failed to deserialize 'updateDTO'. Please ensure it contains valid JSON.");
                }

                if (id != updateDTO.Id)
                {
                    return BadRequest($"Invalid or mismatched ID. URL ID: {id}, DTO ID: {updateDTO.Id}");
                }

                // Optional file handling
                byte[] logoBytes = null;
                byte[] restaurantImageBytes = null;
                var logoImage = Request.Form.Files["logoImage"];
                var restaurantImage = Request.Form.Files["restaurantImage"];

                if (logoImage != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await logoImage.CopyToAsync(memoryStream);
                        logoBytes = memoryStream.ToArray();
                    }
                }
                if (restaurantImage != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await restaurantImage.CopyToAsync(memoryStream);
                        restaurantImageBytes = memoryStream.ToArray();
                    }
                }

                // Pass the DTO and optionally the images to the service layer for processing
                var updatedOutlet = await _outletService.UpdateOutletAsync(updateDTO, logoBytes, restaurantImageBytes);

                if (updatedOutlet == null)
                {
                    return NotFound(new { Message = "Outlet not found. Unable to update the outlet with ID: " + id });
                }

                return Ok(updatedOutlet);
            }
            catch (KeyNotFoundException ex)
            {
                // Return a NotFound response with the custom error message
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception and return a BadRequest with a more generic error message
                return BadRequest($"An error occurred while updating the outlet. Error: {ex.Message}");
            }
        }





        [HttpGet("GetOutletsByOwner/{ownerId}")]
        public async Task<IActionResult> GetOutletsByOwner(Guid ownerId)
        {
            try
            {
                var outlets = await _outletService.GetOutletsByOwner(ownerId);
                return Ok(outlets);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetOutletImages/{outletId}")]
        public async Task<IActionResult> GetOutletImages(int outletId)
        {
            try
            {
                var imagesDto = await _outletService.GetOutletImagesAsync(outletId);
                if (imagesDto != null)
                {
                    return Ok(imagesDto);
                }
                else
                {
                    return NotFound(new { Message = "Outlet images not found" });
                }
            }
            catch (Exception ex)
            {
                // Log the exception, consider using a logging framework
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }


        [HttpPost("RegisterOutlet")]
        public async Task<IActionResult> RegisterOutlet([FromBody] Outlet model, [FromQuery] string currentUserId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var registeredOutlet = await _outletService.RegisterOutletAsync(model, currentUserId);
                return Ok(new { Success = true, Outlet = registeredOutlet });
            }
            catch (ArgumentNullException)
            {
                return BadRequest(new { Success = false, Message = "Invalid outlet data" });
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
     

    }
}
