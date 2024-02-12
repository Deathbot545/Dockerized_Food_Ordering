using Core.DTO;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace Core.Services.OutletSer
{
    public class OutletService : IOutletService
    {
        private readonly AppDbContext _context;

        public OutletService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Outlet>> GetOutletsByOwner(Guid ownerId)
        {
            return await _context.Outlets.Where(x => x.OwnerId == ownerId && !x.IsDeleted).ToListAsync();
        }
        public async Task<Outlet> RegisterOutletAsync(Outlet outlet, string currentUserId)
        {
            if (outlet == null)
            {
                throw new ArgumentNullException(nameof(outlet));
            }

            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new ArgumentNullException(nameof(currentUserId));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Set the OwnerId to the current user's ID
                    outlet.OwnerId = Guid.Parse(currentUserId);

                    // Set DateTime fields
                    outlet.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                    outlet.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

                    // Save Outlet to database to generate ID
                    await _context.Outlets.AddAsync(outlet);
                    SetDateTimePropertiesToUtc(outlet);
                    await _context.SaveChangesAsync();

                    // Create a new Menu and associate it with the Outlet
                    Menu newMenu = new Menu
                    {
                        // Assuming Name, CreatedAt, UpdatedAt, and OutletId are fields in your Menu entity
                        Name = "Default Menu for " + outlet.InternalOutletName,
                      
                        OutletId = outlet.Id  // Set the OutletId to the newly generated Outlet Id
                    };

                    // Save Menu to database
                    await _context.Menu.AddAsync(newMenu);
                    await _context.SaveChangesAsync();

                    // Now set the MenuId in Outlet
                    outlet.MenuId = newMenu.Id;

                    // Update the Outlet entity with the new MenuId
                    _context.Outlets.Update(outlet);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return outlet;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log the exception or handle it
                    throw;
                }
            }
        }

        public async Task<bool> DeleteOutletByIdAsync(int id)
        {
            // Find the outlet first
            var outlet = await _context.Outlets.Include(o => o.Tables).FirstOrDefaultAsync(o => o.Id == id);

            if (outlet == null)
            {
                return false;
            }

            // Get the Menu associated with the Outlet
            var menu = await _context.Menu
                .Include(m => m.MenuCategories)
                .ThenInclude(mc => mc.MenuItems)
                .FirstOrDefaultAsync(m => m.Id == outlet.MenuId);

            if (menu != null)
            {
                // Remove MenuItems and MenuCategories
                foreach (var menuCategory in menu.MenuCategories)
                {
                    // Removing MenuItems
                    _context.RemoveRange(menuCategory.MenuItems);
                }

                // Removing MenuCategories
                _context.RemoveRange(menu.MenuCategories);

                // Remove the menu itself
                _context.Menu.Remove(menu);
            }

            // Remove QRCode and tables
            foreach (var table in outlet.Tables)
            {
                RemoveQRCode(table.Id);
                _context.Tables.Remove(table); // Assuming Tables are already loaded in the outlet entity
            }

            // Now remove the outlet
            _context.Outlets.Remove(outlet);

            await _context.SaveChangesAsync();

            return true;
        }


        public List<Table> GetTablesByOutlet(int outletId)
        {
            return _context.Tables
                .Where(t => t.OutletId == outletId)
                .Include(t => t.QRCode)
                .ToList();
        }
        public bool RemoveQRCode(int tableId)
        {
            var table = _context.Tables.Include(t => t.QRCode).SingleOrDefault(t => t.Id == tableId);
            if (table == null) return false;

            // Remove the associated QRCode entity
            if (table.QRCode != null)
            {
                _context.QRCodes.Remove(table.QRCode);
            }

            // Remove the table itself (optional, uncomment the line below if you wish to remove the table)
            _context.Tables.Remove(table);

            _context.SaveChanges();
            return true;
        }


        public (Table, QRCode) AddTableAndGenerateQRCode(int outletId, string tableName)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Create new table
                    var newTable = new Table
                    {
                        TableIdentifier = tableName,
                        OutletId = outletId
                    };

                    _context.Tables.Add(newTable);
                    _context.SaveChanges(); // This will set the new ID if it's auto-generated

                    // Generate QR Code
                    var qrCode = GenerateQRCodeForTable(outletId, newTable.Id);
                    _context.QRCodes.Add(qrCode);
                    _context.SaveChanges();

                    // Commit the transaction
                    transaction.Commit();

                    return (newTable, qrCode);
                }
                catch (Exception ex)
                {
                    // Log the exception and roll back the transaction
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public QRCode GenerateQRCodeForTable(int outletId, int tableId)
        {
            var qrCodeWriter = new BarcodeWriterSvg
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 300,
                    Height = 300
                }
            };

            // Include table number in the URL
            // Point to your MVC Controller and Action instead of API endpoint
            var urlToEncode = $"https://localhost:7257/Account/SpecialLogin?outletId={outletId}&tableId={tableId}";

            var svgContent = qrCodeWriter.Write(urlToEncode);

            var qrCode = new QRCode
            {
                Data = Encoding.UTF8.GetBytes(svgContent.Content),
                MimeType = "image/svg+xml",
                TableId = tableId  // Assuming QRCode entity now has a TableNumber property
            };

            return qrCode;
        }
       

        public static void SetDateTimePropertiesToUtc(object obj)
        {
            foreach (var property in obj.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(DateTime))
                {
                    var dt = (DateTime)property.GetValue(obj);
                    if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        property.SetValue(obj, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                    }
                }
            }
        }
        public async Task<OutletInfoDTO> GetSpecificOutletInfoByOutletIdAsync(int outletId)
        {
            if (outletId <= 0)
            {
                throw new ArgumentException("Invalid Outlet ID provided", nameof(outletId));
            }

            var outlet = await _context.Outlets
                        .Where(o => o.Id == outletId && !o.IsDeleted)
                        .Select(o => new
                        {
                            o.CustomerFacingName,
                            o.Logo,
                            o.RestaurantImage,
                            o.OperatingHoursStart,
                            o.OperatingHoursEnd,
                            o.Contact
                        })
                        .FirstOrDefaultAsync();

            if (outlet == null)
            {
                throw new InvalidOperationException($"Outlet with ID {outletId} not found.");
            }

            return new OutletInfoDTO
            {
                CustomerFacingName = outlet.CustomerFacingName,
                Logo = outlet.Logo,
                RestaurantImage = outlet.RestaurantImage,
                OperatingHoursStart = outlet.OperatingHoursStart,
                OperatingHoursEnd = outlet.OperatingHoursEnd,
                Contact = outlet.Contact
            };
        }

        public async Task<Outlet> GetOutletBySubdomain(string subdomain)
        {
            return await _context.Outlets.FirstOrDefaultAsync(o => o.Subdomain == subdomain);
        }


    }

}
