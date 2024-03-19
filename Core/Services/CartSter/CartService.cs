using Core.DTO;
using Core.Services.MenuS;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.CartSter
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;
        private readonly IMenuService _menuService;

        public CartService(AppDbContext context,IMenuService menuService)
        {
            _context = context;
            _menuService = menuService;
        }

    }

}
