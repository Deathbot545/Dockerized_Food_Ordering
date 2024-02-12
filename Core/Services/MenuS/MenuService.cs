using Core.DTO;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.MenuS
{
    public class MenuService : IMenuService
    {
        private readonly AppDbContext _context;

        public MenuService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Menu> EnsureMenuExistsAsync(int outletId, string internalOutletName)
        {
            var menu = await _context.Menu.FirstOrDefaultAsync(m => m.OutletId == outletId);

            if (menu == null)
            {
                menu = new Menu
                {
                    OutletId = outletId,
                    Name = internalOutletName
                };
                _context.Menu.Add(menu);
                await _context.SaveChangesAsync();
            }

            return menu;
        }
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.MenuCategories.FindAsync(categoryId);
            if (category == null)
            {
                return false;
            }

            _context.MenuCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MenuCategory> AddCategoryAsync(int menuId, string categoryName)
        {
            var menuCategory = new MenuCategory
            {
                MenuId = menuId,
                Name = categoryName
            };

            _context.MenuCategories.Add(menuCategory);
            await _context.SaveChangesAsync();

            return menuCategory;
        }
        // Method to get all categories
        public async Task<List<MenuCategory>> GetAllCategoriesAsync(int outletId)
        {
            // Fetch the MenuCategories through the Menu's OutletId
            return await _context.MenuCategories
                                 .Include(m => m.Menu)  // Include the Menu information
                                 .Where(c => c.Menu.OutletId == outletId)
                                 .ToListAsync();
        }

        public async Task<MenuItem> AddMenuItemAsync(MenuItemDto menuItemDto)
        {
            byte[] imageBytes = Convert.FromBase64String(menuItemDto.Image);

            MenuItem newMenuItem = new MenuItem
            {
                Name = menuItemDto.Name,
                Description = menuItemDto.Description,
                Price = menuItemDto.Price,
                MenuCategoryId = menuItemDto.MenuCategoryId,
                Image = Convert.FromBase64String(menuItemDto.Image),
            };

            _context.MenuItem.Add(newMenuItem);
            await _context.SaveChangesAsync();

            return newMenuItem;
        }


        public async Task<List<MenuItemDto>> GetMenuItemsByOutletIdAsync(int outletId)
        {
            var menu = await _context.Menu.FirstOrDefaultAsync(m => m.OutletId == outletId);
            if (menu == null)
            {
                return null;
            }

            var menuItems = await _context.MenuItem
                .Include(mi => mi.MenuCategory)  // Include the MenuCategory to access the name
                .Where(mi => mi.MenuCategory.MenuId == menu.Id)
                .Select(mi => new MenuItemDto
                {
                    id= mi.Id,
                    Name = mi.Name,
                    Description = mi.Description,
                    Price = mi.Price,
                    MenuCategoryId = mi.MenuCategoryId,
                    CategoryName = mi.MenuCategory.Name,  // Assign the category name here
                    Image = Convert.ToBase64String(mi.Image)  // Convert byte[] to base64 string
                })
                .ToListAsync();

            return menuItems;
        }

        public async Task<MenuItemDto> GetMenuItemByIdAsync(int menuItemId)
        {
            var menuItem = await _context.MenuItem
                .Include(mi => mi.MenuCategory)
                .Where(mi => mi.Id == menuItemId)
                .Select(mi => new MenuItemDto
                {
                    id = mi.Id,
                    Name = mi.Name,
                    Description = mi.Description,
                    Price = mi.Price,
                    MenuCategoryId = mi.MenuCategoryId,
                    CategoryName = mi.MenuCategory.Name,
                    Image = Convert.ToBase64String(mi.Image)
                })
                .FirstOrDefaultAsync();

            return menuItem;
        }


        public async Task<bool> DeleteMenuItemAsync(int itemId)
        {
            var item = await _context.MenuItem.FindAsync(itemId);
            if (item == null)
            {
                return false;
            }

            _context.MenuItem.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

    }

}
