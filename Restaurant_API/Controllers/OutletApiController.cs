
using Microsoft.AspNetCore.Mvc;
using ZXing.QrCode.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json;
using System.Net.Http;
using Restaurant_API.Services.OutletSer;
using Restaurant_API.DTO;
using Restaurant_API.Models;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class OutletApiController : ControllerBase
    {
        private readonly IOutletService _outletService;
        private readonly ILogger<OutletApiController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public OutletApiController(IOutletService outletService, ILogger<OutletApiController> logger, IHttpClientFactory httpClientFactory)
        {
            _outletService = outletService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }


        [HttpGet("outletInfo/{id}")]
        public async Task<IActionResult> GetOutletInfo(int id)
        {
            try
            {
                OutletInfoDTO result = await _outletService.GetSpecificOutletInfoByOutletIdAsync(id);
                return Ok(result);
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(argEx.Message);
            }
            catch (InvalidOperationException invOpEx)
            {
                return NotFound(invOpEx.Message);
            }
            catch (Exception ex)
            {
                // Handle/log the exception as per your application's requirements
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetAllOutletsAsync")]
        public async Task<IActionResult> GetAllOutletsAsync()
        {
            try
            {
                var outlets = await _outletService.GetAllOutletsAsync();
                if (outlets == null || !outlets.Any())
                {
                    return NotFound(new { Message = "No outlets found." });
                }

                return Ok(outlets);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "An error occurred", Details = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteOutlet(int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var menuApiUrl = $"http://menu-api-service/api/menus/outlet/{id}";
                var response = await httpClient.DeleteAsync(menuApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to delete menus for outlet {id}.");
                    // Handle failure as appropriate, possibly returning an error response
                }

                // Assuming _outletService.DeleteOutletByIdAsync(id) correctly deletes the outlet
                var result = await _outletService.DeleteOutletByIdAsync(id);
                if (result)
                {
                    return Ok(new { Message = "Outlet and associated menus successfully deleted" });
                }
                else
                {
                    return NotFound(new { Message = "Outlet not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the outlet.");
                return StatusCode(500, new { Message = "An error occurred", Details = ex.Message });
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
                if (outlets == null || !outlets.Any())
                {
                    return Ok(new List<Outlet>());  // Return an empty list instead of causing an error
                }
                return Ok(outlets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching outlets for owner ID {OwnerId}", ownerId);
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

        [HttpGet("GetTablesByOutlet/{outletId}")]
        public IActionResult GetTablesByOutlet(int outletId)
        {
            try
            {
                var tables = _outletService.GetTablesByOutlet(outletId);
                var tableDTOs = tables.Select(t => new TableDto
                {
                    Id = t.Id,
                    TableIdentifier = t.TableIdentifier
                    // Map other properties as needed
                }).ToList();

                return Ok(tableDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while fetching tables for outlet {outletId}: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
