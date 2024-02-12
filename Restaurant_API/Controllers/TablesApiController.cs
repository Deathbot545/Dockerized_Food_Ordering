using Core.DTO;
using Core.Services.OutletSer;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TablesApiController : Controller
    {
        private readonly IOutletService _outletService;

        public TablesApiController(IOutletService outletService)
        {
            _outletService = outletService;
        }
        [HttpGet("GetTables")]
        public ActionResult<List<TableDto>> GetTablesByOutlet(int id)
        {
            var tables = _outletService.GetTablesByOutlet(id);
            var tableDtos = tables.Select(TableDtoAndConverter.TableToDto).ToList(); // Convert to DTOs, now with Id included
            return tableDtos;
        }

        // Service remains the same

        [HttpPost("AddTable")]
        public IActionResult AddTable([FromBody] AddTableDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var (newTable, qrCode) = _outletService.AddTableAndGenerateQRCode(model.OutletId, model.TableIdentifier);

                return Ok(new { Message = "Table and QR code added successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception here for debugging
                Console.WriteLine($"Exception: {ex}");
                return StatusCode(500, "Internal Server Error");
            }
        }


        [HttpDelete("RemoveQRCode")]
        public IActionResult RemoveQRCode(int id)
        {
            var result = _outletService.RemoveQRCode(id);
            if (result)
            {
                return Ok(new { Message = "QR code removed successfully" });
            }
            else
            {
                return NotFound(new { Message = "Table not found" });
            }
        }


    }
}
