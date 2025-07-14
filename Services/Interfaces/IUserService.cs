using ALGASystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ALGASystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
        Task<IdentityResult> DeleteUserAsync(string userId);
        Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName);
        Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName);
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<bool> HasPermissionAsync(string userId, string permissionName);
        Task<IdentityResult> AssignPermissionToUserAsync(string userId, int permissionId);
        Task<IdentityResult> RemovePermissionFromUserAsync(string userId, int permissionId);
        Task<IEnumerable<Permission>> GetEffectivePermissionsAsync(string userId);
        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);
        Task LogActivityAsync(string userId, string action);
    }
}
