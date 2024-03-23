using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class OutletDbContext : DbContext
    {
        public OutletDbContext(DbContextOptions<OutletDbContext> options) : base(options)
        {
        }

        public DbSet<Outlet> Outlets { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Model configurations here
        }
    }
}
