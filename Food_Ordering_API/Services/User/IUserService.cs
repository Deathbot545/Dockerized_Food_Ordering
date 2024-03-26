
using Food_Ordering_API.DTO;
using Food_Ordering_API.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Food_Ordering_API.Services.User
{
    public interface IUserService
    {
        Task<UserProfileModel> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, UserProfileUpdateDTO model);
    }
}
