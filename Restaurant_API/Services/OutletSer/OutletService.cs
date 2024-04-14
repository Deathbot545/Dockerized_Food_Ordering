
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant_API.Data;
using Restaurant_API.DTO;
using Restaurant_API.Models;
using Restaurant_API.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace Restaurant_API.Services.OutletSer
{
    public class OutletService : IOutletService
    {
        private readonly OutletDbContext _context;

        public OutletService(OutletDbContext context)
        {
            _context = context;
        }

        public async Task<List<OutletInfoDTO>> GetAllOutletsAsync()
        {
            // Assuming `_context.Outlets` is your DBSet<Outlet>
            return await _context.Outlets.Select(o => new OutletInfoDTO
            {
                CustomerFacingName = o.CustomerFacingName,
                Logo = o.Logo, // You'll likely need to convert this to a suitable format for JSON
                RestaurantImage = o.RestaurantImage, // Same as above
                OperatingHoursStart = o.OperatingHoursStart,
                OperatingHoursEnd = o.OperatingHoursEnd,
                Contact = o.Contact,
                Country = o.Country,
                City = o.City
            }).ToListAsync();
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

                  

                    await _context.SaveChangesAsync();

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

        public async Task<Outlet> UpdateOutletAsync(OutletUpdateDTO updateDTO, byte[]? logoImage = null, byte[]? restaurantImage = null)
        {
            var outlet = await _context.Outlets.FirstOrDefaultAsync(o => o.Id == updateDTO.Id);
            if (outlet == null)
            {
                // Throw a custom exception if the outlet is not found
                throw new KeyNotFoundException($"Outlet with ID {updateDTO.Id} not found.");
            }


            // Update fields based on DTO
            outlet.InternalOutletName = updateDTO.InternalOutletName ?? outlet.InternalOutletName;
            outlet.CustomerFacingName = updateDTO.CustomerFacingName ?? outlet.CustomerFacingName;
            outlet.BusinessType = updateDTO.BusinessType ?? outlet.BusinessType;
            outlet.Country = updateDTO.Country ?? outlet.Country;
            outlet.City = updateDTO.City ?? outlet.City;
            outlet.State = updateDTO.State ?? outlet.State;
            outlet.Zip = updateDTO.Zip ?? outlet.Zip;
            outlet.StreetAddress = updateDTO.StreetAddress ?? outlet.StreetAddress;
            outlet.PostalCode = updateDTO.PostalCode ?? outlet.PostalCode;
            outlet.DateOpened = updateDTO.DateOpened ?? outlet.DateOpened;
            outlet.Description = updateDTO.Description ?? outlet.Description;
            outlet.EmployeeCount = updateDTO.EmployeeCount ?? outlet.EmployeeCount;
            outlet.OperatingHoursStart = updateDTO.OperatingHoursStart ?? outlet.OperatingHoursStart;
            outlet.OperatingHoursEnd = updateDTO.OperatingHoursEnd ?? outlet.OperatingHoursEnd;
            outlet.Contact = updateDTO.Contact ?? outlet.Contact;

            // Update images if new images are provided
            if (logoImage != null && logoImage.Length > 0)
            {
                outlet.Logo = logoImage;
            }
            if (restaurantImage != null && restaurantImage.Length > 0)
            {
                outlet.RestaurantImage = restaurantImage;
            }

            SetDateTimePropertiesToUtc(outlet);
            outlet.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            _context.Outlets.Update(outlet);
            await _context.SaveChangesAsync();

            return outlet;
        }

        public async Task<bool> DeleteOutletByIdAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var outlet = await _context.Outlets.Include(o => o.Tables)
                                                   .ThenInclude(t => t.QRCode)
                                                   .Include(o => o.KitchenStaffs)
                                                   .SingleOrDefaultAsync(o => o.Id == id);
                if (outlet == null) return false;

                // Manually delete related QR codes (if any)
                foreach (var table in outlet.Tables)
                {
                    if (table.QRCode != null)
                    {
                        _context.QRCodes.Remove(table.QRCode);
                    }
                }

                // Manually delete related tables
                _context.Tables.RemoveRange(outlet.Tables);

                // Manually delete related kitchen staff
                _context.KitchenStaffs.RemoveRange(outlet.KitchenStaffs);

                // Finally, delete the outlet itself
                _context.Outlets.Remove(outlet);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception
                throw; // Re-throw the exception to be handled by the caller or global exception handler
            }
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
            var urlToEncode = $"https://restosolutionssaas.com/Account/SpecialLogin?outletId={outletId}&tableId={tableId}";

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
                        .Select(o => new OutletInfoDTO // Directly project to OutletInfoDTO
                        {
                            CustomerFacingName = o.CustomerFacingName,
                            Logo = o.Logo,
                            RestaurantImage = o.RestaurantImage,
                            OperatingHoursStart = o.OperatingHoursStart,
                            OperatingHoursEnd = o.OperatingHoursEnd,
                            Contact = o.Contact,
                            Country = o.Country,
                            City = o.City,
                            // Add any other fields you need
                        })
                        .FirstOrDefaultAsync();

            if (outlet == null)
            {
                throw new InvalidOperationException($"Outlet with ID {outletId} not found.");
            }

            return outlet;
        }

        public async Task<OutletImagesDTO> GetOutletImagesAsync(int outletId)
        {
            var outlet = await _context.Outlets
                .Where(o => o.Id == outletId && !o.IsDeleted)
                .Select(o => new { o.Logo, o.RestaurantImage })
                .FirstOrDefaultAsync();

            if (outlet == null)
            {
                throw new InvalidOperationException($"Outlet with ID {outletId} not found.");
            }

            // Convert byte array image data to Base64 strings
            var imagesDto = new OutletImagesDTO
            {
                LogoBase64 = outlet.Logo != null ? Convert.ToBase64String(outlet.Logo) : null,
                RestaurantImageBase64 = outlet.RestaurantImage != null ? Convert.ToBase64String(outlet.RestaurantImage) : null,
            };

            return imagesDto;
        }

        public async Task<Outlet> GetOutletBySubdomain(string subdomain)
        {
            return await _context.Outlets.FirstOrDefaultAsync(o => o.Subdomain == subdomain);
        }

        public async Task<bool> AddKitchenStaffAsync(KitchenStaffViewModel model)
        {
            try
            {
                // Create an instance of PasswordHasher
                var passwordHasher = new PasswordHasher<KitchenStaff>();

                var newStaff = new KitchenStaff
                {
                    Name = model.Name,
                    Email = model.Email,
                    OutletId = model.OutletId, // Assuming you pass the OutletId from the form
                    PasswordHash = passwordHasher.HashPassword(null, model.Password), // Hash the plain password
                    Role = model.Role // Set the Role property
                };

                _context.KitchenStaffs.Add(newStaff);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<IEnumerable<KitchenStaffViewModel>> GetKitchenStaffByOutletAsync(int outletId)
        {
            return await _context.KitchenStaffs
                                 .Where(k => k.OutletId == outletId)
                                 .Select(k => new KitchenStaffViewModel
                                 {
                                     Id = k.Id, // Include the ID here
                                     Name = k.Name,
                                     Email = k.Email,
                                     Role = k.Role,
                                     OutletId = outletId
                                     // other properties as needed
                                 })
                                 .ToListAsync();
        }

        public async Task<bool> DeleteKitchenStaffAsync(int id)
        {
            var staffMember = await _context.KitchenStaffs.FindAsync(id);
            if (staffMember != null)
            {
                _context.KitchenStaffs.Remove(staffMember);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateKitchenStaffAsync(KitchenStaffUpdateViewModel model)
        {
            var staffMember = await _context.KitchenStaffs.FirstOrDefaultAsync(k => k.Id == model.Id);
            if (staffMember == null)
            {
                return false;
            }

            staffMember.Name = model.Name;
            staffMember.Email = model.Email;
            // Assuming you have a method to handle roles, perhaps through an enum or separate entity
            staffMember.Role = model.Role;

            _context.KitchenStaffs.Update(staffMember);
            await _context.SaveChangesAsync();

            return true;
        }


    }

}
