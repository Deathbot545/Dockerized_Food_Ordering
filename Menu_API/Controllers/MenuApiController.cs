﻿using Menu_API.DTO;
using Menu_API.Services.MenuS;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Menu_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowMyOrigins")]
    public class MenuApiController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly ILogger<MenuApiController> _logger;
        

        public MenuApiController(IMenuService menuService,ILogger<MenuApiController> logger)
        {
            _menuService = menuService;
            _logger = logger;
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

                var newMenuCategory = await _menuService.AddCategoryAsync(menu.Id, menuCategoryDto.CategoryName, menuCategoryDto.ExtraItems);

                return Ok(newMenuCategory); // Return the newly created category along with extras
            }
            catch (Exception ex)
            {
                // Log the exception details
                // You can also return the exception message to help with debugging
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
        [HttpGet("GetCategoryWithExtraItems/{categoryId}")]
        public async Task<IActionResult> GetCategoryWithExtraItems(int categoryId)
        {
            var categoryDto = await _menuService.GetCategoryWithExtraItemsAsync(categoryId);
            if (categoryDto == null)
            {
                return NotFound(new { message = "Category not found." });
            }
            return Ok(categoryDto);
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

                var newMenuItem = await _menuService.AddMenuItemAsync(menuItemDto);
                return Json(new
                {
                    newMenuItem.Id,
                    newMenuItem.Name,
                    newMenuItem.Description,
                    newMenuItem.Price,
                    newMenuItem.IsVegetarian,
                    newMenuItem.MenuCategoryId,
                    newMenuItem.Image,
                    Sizes = newMenuItem.MenuItemSizes.Select(size => new
                    {
                        size.Size,
                        size.Price
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
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
        [HttpGet("GetMenuItemsWithExtras/{outletId}")]
        public async Task<IActionResult> GetMenuItemsWithExtras(int outletId)
        {
            var menuItems = await _menuService.GetMenuItemsWithExtrasByOutletIdAsync(outletId);
            if (menuItems == null)
            {
                return NotFound();
            }
            return Ok(menuItems);
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

        [HttpDelete("DeleteMenusByOutlet/{outletId}")]
        public async Task<IActionResult> DeleteMenusByOutlet(int outletId)
        {
            try
            {
                var isSuccess = await _menuService.DeleteMenusByOutletIdAsync(outletId);
                if (isSuccess)
                {
                    return Ok(new { Message = "All menus for the outlet were deleted successfully." });
                }
                else
                {
                    // Change this to return an OK status even if no menus were found
                    return Ok(new { Message = "No menus found for the outlet, but the operation completed successfully." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while trying to delete menus for outlet {outletId}.");
                return StatusCode(500, new { Message = "An error occurred while deleting the menus.", Details = ex.Message });
            }
        }


    }
}
