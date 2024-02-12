using Core.DTO;
using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.OutletSer
{
    public interface IOutletService
    {
        Task<Outlet> RegisterOutletAsync(Outlet outlet, string currentUserId);
        Task<List<Outlet>> GetOutletsByOwner(Guid ownerId);
        List<Table> GetTablesByOutlet(int outletId);
        (Table, QRCode) AddTableAndGenerateQRCode(int outletId, string tableName);
        bool RemoveQRCode(int tableId);
        Task<bool> DeleteOutletByIdAsync(int id);
       Task<OutletInfoDTO> GetSpecificOutletInfoByOutletIdAsync(int outletId);
        Task<Outlet> GetOutletBySubdomain(string subdomain);

        // Add other methods related to Outlet management
    }

}
