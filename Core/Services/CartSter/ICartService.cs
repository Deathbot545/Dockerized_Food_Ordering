using Core.DTO;
using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.CartSter
{
    public interface ICartService
    {
        Task<int> AddToCartAsync(MenuItem menuItem, int quantity, string userId = null, int tableId = 0, int outletId = 0);
        Task<int> ProcessCartRequestAsync(CartRequest request);
        // Add other cart related methods if needed...
    }
}
