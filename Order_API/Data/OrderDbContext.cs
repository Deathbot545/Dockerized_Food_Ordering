using Order_API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order_API.Data
{
    public class OrderDbContext : DbContext
    {
      /*  public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            modelBuilder.Entity<OrderDetail>()
       .HasMany(od => od.ExtraItems)
       .WithOne(ei => ei.OrderDetail)
       .HasForeignKey(ei => ei.OrderDetailId);

            base.OnModelCreating(modelBuilder);
            // Model configurations here, e.g., modelBuilder.Entity<Order>().ToTable("Orders");
        }*/
    }
}
