using Core.DTO;
using Core.Services.MenuS;
using Core.Services.OutletSer;
using Infrastructure.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Restaurant_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class MenuApiController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly IOutletService _outletService;

        public MenuApiController(IMenuService menuService,IOutletService outletService)
        {
            _menuService = menuService;
            _outletService = outletService;
        }

        [HttpPost("AddCategory")]
        public async Task<IActionResult> AddCategory([FromBody] MenuCategoryDto menuCategoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var menu = await _menuService.EnsureMenuExistsAsync(menuCategoryDto.OutletId, menuCategoryDto.InternalOutletName);

                var newMenuCategory = await _menuService.AddCategoryAsync(menu.Id, menuCategoryDto.CategoryName);

                return Ok("Test");
            }
            catch (Exception ex)
            {
                // Log the exception details
                // You can also return the exception message to help with debugging
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
        // API to get all categories
        [HttpGet("GetAllCategories/{outletId}")]
        public async Task<IActionResult> GetAllCategories(int outletId)
        {
            var categories = await _menuService.GetAllCategoriesAsync(outletId);
            var categoryDtos = categories.Select(c => new MenuCategoryDto
            {
                Id = c.Id,
                OutletId = outletId,
                CategoryName = c.Name
    }).ToList();

            return Ok(categoryDtos);
        }
        [HttpDelete("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _menuService.DeleteCategoryAsync(id);
            if (result)
            {
                return Ok(new { message = "Category deleted successfully." });
            }
            return NotFound(new { message = "Category not found." });
        }
        [HttpPost("AddMenuItem")]
        public async Task<IActionResult> AddMenuItem([FromBody] MenuItemDto menuItemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                byte[] imageBytes = Convert.FromBase64String(menuItemDto.Image);
                var newMenuItem = await _menuService.AddMenuItemAsync(menuItemDto);

                return Json(newMenuItem);
            }
            catch (Exception ex)
            {
                // Log the exception details
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
        [HttpGet("GetMenuItems/{outletId}")]               //MenuItem By Outlet ID
        public async Task<IActionResult> GetMenuItems(int outletId)
        {
            try
            {
                var menuItems = await _menuService.GetMenuItemsByOutletIdAsync(outletId);
                if (menuItems == null)
                {
                    return NotFound(new { message = "Outlet or Menu not found." });
                }

                return Ok(menuItems);
            }
            catch (Exception ex)
            {
                // Log the exception details
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
        [HttpGet("GetMenuItem/{menuItemId}")]              //MenuItem by Menu Item ID
        public async Task<IActionResult> GetMenuItem(int menuItemId)
        {
            try
            {
                var menuItem = await _menuService.GetMenuItemByIdAsync(menuItemId);
                if (menuItem == null)
                {
                    return NotFound(new { message = "Menu item not found." });
                }

                return Ok(menuItem);
            }
            catch (Exception ex)
            {
                // Log the exception details
                return StatusCode(500, $"Internal server error: {ex}");
            }
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
        [HttpDelete("DeleteMenuItem/{itemId}")]
        public async Task<IActionResult> DeleteMenuItem(int itemId)
        {
            try
            {
                bool isDeleted = await _menuService.DeleteMenuItemAsync(itemId);
                if (isDeleted)
                {
                    return Ok(new { message = "Item deleted successfully." });
                }
                else
                {
                    return NotFound(new { message = "Item not found." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


    }
}
