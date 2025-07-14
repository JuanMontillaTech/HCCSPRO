using ALGASystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ALGASystem.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<ApplicationRole>> GetAllRolesAsync();
        Task<ApplicationRole> GetRoleByIdAsync(string roleId);
        Task<ApplicationRole> GetRoleByNameAsync(string roleName);
        Task<IdentityResult> CreateRoleAsync(ApplicationRole role);
        Task<IdentityResult> UpdateRoleAsync(ApplicationRole role);
        Task<IdentityResult> DeleteRoleAsync(string roleId);
        Task<IdentityResult> AssignPermissionToRoleAsync(string roleId, int permissionId);
        Task<IdentityResult> RemovePermissionFromRoleAsync(string roleId, int permissionId);
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId);
    }
}
