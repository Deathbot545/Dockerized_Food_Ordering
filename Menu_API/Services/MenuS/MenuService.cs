using Menu_API.Services.MenuS;
using Menu_API.Data;
using Menu_API.Models;
using Microsoft.EntityFrameworkCore;
using Menu_API.DTO;

namespace Menu_API.Services.MenuS
{
    public class MenuService : IMenuService
    {
        private readonly MenuDbContext _context;
        private readonly ILogger<MenuService> _logger;

        public MenuService(MenuDbContext context, ILogger<MenuService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Menu> EnsureMenuExistsAsync(int outletId, string internalOutletName)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.OutletId == outletId);

            if (menu == null)
            {
                menu = new Menu
                {
                    OutletId = outletId,
                    Name = internalOutletName
                };
                _context.Menus.Add(menu);
                await _context.SaveChangesAsync();
            }

            return menu;
        }
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.MenuCategories
                                         .Include(c => c.ExtraItems)
                                         .FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category == null)
            {
                return false;
            }

            _context.ExtraItems.RemoveRange(category.ExtraItems); // Delete related extra items
            _context.MenuCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<MenuCategoryDto> AddCategoryAsync(int menuId, string categoryName, List<ExtraItemDto> extraItems)
        {
            var menuCategory = new MenuCategory
            {
                MenuId = menuId,
                Name = categoryName,
                ExtraItems = extraItems.Select(e => new ExtraItem
                {
                    Name = e.Name,
                    Price = e.Price
                }).ToList()
            };

            _context.MenuCategories.Add(menuCategory);
            await _context.SaveChangesAsync();

            var menuCategoryDto = new MenuCategoryDto
            {
                Id = menuCategory.Id,
                OutletId = menuCategory.Menu.OutletId.HasValue ? menuCategory.Menu.OutletId.Value : 0, // or handle it appropriately
                CategoryName = menuCategory.Name,
                InternalOutletName = menuCategory.Menu.Name,
                ExtraItems = menuCategory.ExtraItems.Select(ei => new ExtraItemDto
                {
                    Id = ei.Id,
                    Name = ei.Name,
                    Price = ei.Price
                }).ToList()
            };


            return menuCategoryDto;
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
        public async Task<MenuCategoryDto> GetCategoryWithExtraItemsAsync(int categoryId)
        {
            var category = await _context.MenuCategories
                                         .Include(c => c.Menu)
                                         .Include(c => c.ExtraItems)
                                         .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                return null;
            }

            var categoryDto = new MenuCategoryDto
            {
                Id = category.Id,
                OutletId = category.Menu.OutletId.HasValue ? category.Menu.OutletId.Value : 0,
                CategoryName = category.Name,
                InternalOutletName = category.Menu.Name,
                ExtraItems = category.ExtraItems.Select(ei => new ExtraItemDto
                {
                    Id = ei.Id,
                    Name = ei.Name,
                    Price = ei.Price
                }).ToList()
            };

            return categoryDto;
        }


        public async Task<MenuItem> AddMenuItemAsync(MenuItemDto menuItemDto)
        {
            byte[] imageBytes = Convert.FromBase64String(menuItemDto.Image);

            MenuItem newMenuItem = new MenuItem
            {
                Name = menuItemDto.Name,
                Description = menuItemDto.Description,
                Price = menuItemDto.Price,
                IsVegetarian = menuItemDto.IsVegetarian,
                MenuCategoryId = menuItemDto.MenuCategoryId,
                Image = imageBytes,
                MenuItemSizes = menuItemDto.Sizes.Select(sizeDto => new MenuItemSize
                {
                    Size = sizeDto.Size,
                    Price = sizeDto.Price
                }).ToList()
            };

            _context.MenuItems.Add(newMenuItem);
            await _context.SaveChangesAsync();

            return await _context.MenuItems
                .Include(mi => mi.MenuItemSizes)
                .FirstOrDefaultAsync(mi => mi.Id == newMenuItem.Id);
        }


        public async Task<List<MenuItemWithExtrasDto>> GetMenuItemsWithExtrasByOutletIdAsync(int outletId)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.OutletId == outletId);
            if (menu == null)
            {
                return null;
            }

            var menuItems = await _context.MenuItems
                .Include(mi => mi.MenuCategory)  // Include the MenuCategory to access the name
                .Include(mi => mi.MenuItemSizes) // Include the MenuItemSizes
                .ThenInclude(size => size.MenuItem)
                .Include(mi => mi.MenuCategory.ExtraItems) // Include ExtraItems
                .Where(mi => mi.MenuCategory.MenuId == menu.Id)
                .Select(mi => new MenuItemWithExtrasDto
                {
                    Id = mi.Id,
                    Name = mi.Name,
                    Description = mi.Description,
                    Price = mi.Price,
                    MenuCategoryId = mi.MenuCategoryId,
                    CategoryName = mi.MenuCategory.Name,  // Assign the category name here
                    Image = mi.Image != null ? Convert.ToBase64String(mi.Image) : null,  // Convert byte[] to base64 string
                    Sizes = mi.MenuItemSizes.Select(size => size.Size).ToList(),
                    ExtraItems = mi.MenuCategory.ExtraItems.Select(extra => new ExtraItemDto
                    {
                        Id = extra.Id,
                        Name = extra.Name,
                        Price = extra.Price
                    }).ToList()
                })
                .ToListAsync();

            return menuItems;
        }


        public async Task<List<MenuItemDto>> GetMenuItemsByOutletIdAsync(int outletId)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.OutletId == outletId);
            if (menu == null)
            {
                return null;
            }

            var menuItems = await _context.MenuItems
                .Include(mi => mi.MenuCategory)  // Include the MenuCategory to access the name
                .Include(mi => mi.MenuItemSizes) // Include the MenuItemSizes
                .Where(mi => mi.MenuCategory.MenuId == menu.Id)
                .Select(mi => new MenuItemDto
                {
                    Id = mi.Id,
                    Name = mi.Name,
                    Description = mi.Description,
                    Price = mi.Price,
                    IsVegetarian = mi.IsVegetarian, // Include vegetarian option
                    MenuCategoryId = mi.MenuCategoryId,
                    CategoryName = mi.MenuCategory.Name,  // Assign the category name here
                    Image = Convert.ToBase64String(mi.Image),  // Convert byte[] to base64 string
                    Sizes = mi.MenuItemSizes.Select(size => new MenuItemSizeDto
                    {
                        Size = size.Size,
                        Price = size.Price
                    }).ToList()
                })
                .ToListAsync();

            return menuItems;
        }



        public async Task<MenuItemDto> GetMenuItemByIdAsync(int menuItemId)
        {
            var menuItem = await _context.MenuItems
                .Include(mi => mi.MenuCategory)
                .Include(mi => mi.MenuItemSizes)
                .Where(mi => mi.Id == menuItemId)
                .Select(mi => new MenuItemDto
                {
                    Id = mi.Id,
                    Name = mi.Name,
                    Description = mi.Description,
                    Price = mi.Price,
                    IsVegetarian = mi.IsVegetarian, // Include vegetarian option
                    MenuCategoryId = mi.MenuCategoryId,
                    CategoryName = mi.MenuCategory.Name,
                    Image = Convert.ToBase64String(mi.Image),
                    Sizes = mi.MenuItemSizes.Select(size => new MenuItemSizeDto
                    {
                        Size = size.Size,
                        Price = size.Price
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return menuItem;
        }


        public async Task<bool> DeleteMenuItemAsync(int itemId)
        {
            var item = await _context.MenuItems
                                     .Include(mi => mi.MenuItemSizes)
                                     .FirstOrDefaultAsync(mi => mi.Id == itemId);

            if (item == null)
            {
                return false;
            }

            // Manually delete associated MenuItemSizes
            _context.MenuItemsSize.RemoveRange(item.MenuItemSizes);

            // Remove the MenuItem
            _context.MenuItems.Remove(item);

            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<bool> DeleteMenusByOutletIdAsync(int outletId)
        {
            try
            {
                // Fetch all menus for the given outlet ID
                var menus = await _context.Menus
                                          .Include(m => m.MenuCategories)
                                            .ThenInclude(mc => mc.MenuItems)
                                          .Where(m => m.OutletId == outletId)
                                          .ToListAsync();

                if (!menus.Any())
                {
                    return false;
                }

                // Remove all related menu items and categories
                foreach (var menu in menus)
                {
                    foreach (var category in menu.MenuCategories)
                    {
                        _context.MenuItems.RemoveRange(category.MenuItems);
                    }
                    _context.MenuCategories.RemoveRange(menu.MenuCategories);
                }
                _context.Menus.RemoveRange(menus);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete menus for outlet {OutletId}.", outletId);
                return false;
            }
        }

    }

}
