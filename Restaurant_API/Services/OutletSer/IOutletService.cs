﻿
using Restaurant_API.DTO;
using Restaurant_API.Models;
using Restaurant_API.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant_API.Services.OutletSer
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
        Task<Outlet> UpdateOutletAsync(OutletUpdateDTO updateDTO, byte[]? logoImage = null, byte[]? restaurantImage = null);
        Task<OutletImagesDTO> GetOutletImagesAsync(int outletId);
        Task<Outlet> GetOutletBySubdomain(string subdomain);
        Task<bool> AddKitchenStaffAsync(KitchenStaffViewModel model);
        Task<IEnumerable<KitchenStaffViewModel>> GetKitchenStaffByOutletAsync(int outletId);
        Task<bool> DeleteKitchenStaffAsync(int id);
        Task<bool> UpdateKitchenStaffAsync(KitchenStaffUpdateViewModel model);
        Task<List<OutletInfoDTO>> GetAllOutletsAsync();
        Task<TableDto> GetTableByIdAsync(int tableId);
        // Add other methods related to Outlet management
    }

}
