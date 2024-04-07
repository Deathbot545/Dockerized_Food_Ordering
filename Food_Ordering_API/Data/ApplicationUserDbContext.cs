
using Food_Ordering_API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Food_Ordering_API.Data
{
    public class ApplicationUserDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationUserDbContext(DbContextOptions options) : base(options)
        {
        }

        public class ApplicationUserDbContextFactory : IDesignTimeDbContextFactory<ApplicationUserDbContext>
        {
            public ApplicationUserDbContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationUserDbContext>();
                optionsBuilder.UseNpgsql("ApplicationDbConnection");

                return new ApplicationUserDbContext(optionsBuilder.Options);
            }
        }
        // No need to explicitly declare DbSets for IdentityUser, as IdentityDbContext includes them.
        // Add any additional DbSets or configurations for other entities related to user management, if any.

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Custom configurations here
        }
    }
}
