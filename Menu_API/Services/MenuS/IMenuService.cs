﻿
using Menu_API.DTO;
using Menu_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menu_API.Services.MenuS
{
    public interface IMenuService
    {
        Task<Menu> EnsureMenuExistsAsync(int outletId, string internalOutletName);
        Task<MenuCategoryDto> AddCategoryAsync(int menuId, string categoryName, List<ExtraItemDto> extraItems);
        Task<MenuCategoryDto> GetCategoryWithExtraItemsAsync(int categoryId);
        Task<List<MenuCategory>> GetAllCategoriesAsync(int outletId);
        Task<bool> DeleteCategoryAsync(int categoryId);
        Task<MenuItem> AddMenuItemAsync(MenuItemDto menuItemDto);
        Task<List<MenuItemDto>> GetMenuItemsByOutletIdAsync(int outletId);
        Task<List<MenuItemWithExtrasDto>> GetMenuItemsWithExtrasByOutletIdAsync(int outletId);
        Task<MenuItemDto> GetMenuItemByIdAsync(int menuItemId);
        Task<bool> DeleteMenuItemAsync(int itemId);
        Task<bool> DeleteMenusByOutletIdAsync(int outletId);
    }
}
