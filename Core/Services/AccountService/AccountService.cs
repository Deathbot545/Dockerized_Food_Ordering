using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.AccountService
{
    public class AccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountService> _logger;

        public AccountService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;

        }

        public async Task<(bool Success, IEnumerable<string> Errors, ApplicationUser User)> AddUserAsync(string username, string password, string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            var user = new ApplicationUser { UserName = username, Email = username };
            var result = string.IsNullOrEmpty(password)
                         ? await _userManager.CreateAsync(user)
                         : await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                // No need to call LoginAsync, as user is just created and not yet logged in
                return (true, null, user);
            }

            return (false, result.Errors.Select(e => e.Description), null);
        }




        public async Task<ApplicationUser> LoginAsync(string identifier, string password)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(identifier)
                                    ?? await _userManager.FindByNameAsync(identifier);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                throw new InvalidLoginException();
            }
            _logger.LogInformation($"User found: {user.UserName}, validating password.");
            return user;
        }

        public async Task<ApplicationUser> FindUserAsync(string identifier)
        {
            return await _userManager.FindByEmailAsync(identifier) ?? await _userManager.FindByNameAsync(identifier);
        }

        // Custom exceptions to better convey specific error scenarios
        public class IdentityException : Exception
        {
            public IEnumerable<IdentityError> Errors { get; }

            public IdentityException(IEnumerable<IdentityError> errors)
            {
                Errors = errors;
            }
        }

        public class InvalidLoginException : Exception { }

        public class UserNotFoundException : Exception { }
    }
}
