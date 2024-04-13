
using Food_Ordering_API.DTO;
using Food_Ordering_API.Models;
using Food_Ordering_API.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Food_Ordering_API.Services.User
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<bool> UpdateSubscriptionStatusAsync(string userId, bool isSubscribed)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsSubscribed = isSubscribed;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
        public async Task<UserProfileModel> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return null;
            }

            return new UserProfileModel
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber=user.PhoneNumber
                // Map other fields as needed
            };
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileUpdateDTO model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Apply changes if the fields are not null
            if (model.UserName != null)
            {
                user.UserName = model.UserName;
            }
            if (model.Email != null)
            {
                user.Email = model.Email;
            }
            if (model.PhoneNumber != null)
            {
                user.PhoneNumber = model.PhoneNumber;
            }
            // Continue for other fields...

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }


    }
}
