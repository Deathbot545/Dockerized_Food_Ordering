using Core.DTO;
using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.MenuS
{
    public interface IMenuService
    {
        Task<Menu> EnsureMenuExistsAsync(int outletId, string internalOutletName);
        Task<MenuCategory> AddCategoryAsync(int menuId, string categoryName);
        Task<List<MenuCategory>> GetAllCategoriesAsync(int outletId);
        Task<bool> DeleteCategoryAsync(int categoryId);
        Task<MenuItem> AddMenuItemAsync(MenuItemDto menuItemDto);
        Task<List<MenuItemDto>> GetMenuItemsByOutletIdAsync(int outletId);
        Task<MenuItemDto> GetMenuItemByIdAsync(int menuItemId);
        Task<bool> DeleteMenuItemAsync(int itemId);
    }
}
