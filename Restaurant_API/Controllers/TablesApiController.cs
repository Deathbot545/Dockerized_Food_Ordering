
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Restaurant_API.DTO;
using Restaurant_API.Services.OutletSer;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TablesApiController : Controller
    {
        private readonly ILogger<TablesApiController> _logger;
        private readonly IOutletService _outletService;

        public TablesApiController(IOutletService outletService, ILogger<TablesApiController> logger)
        {
            _outletService = outletService;
            _logger = logger;
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
            _logger.LogInformation("Attempting to add a new table with identifier: {TableIdentifier}", model.TableIdentifier);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid. Model: {@Model}", model);
                return BadRequest(ModelState);
            }

            try
            {
                var (newTable, qrCode) = _outletService.AddTableAndGenerateQRCode(model.OutletId, model.TableIdentifier);

                _logger.LogInformation("Table and QR code added successfully. Table ID: {TableId}", newTable.Id);
                return Ok(new { Message = "Table and QR code added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a new table. Model: {@Model}", model);
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
