using ALGASystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ALGASystem.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();
        Task<Permission> GetPermissionByIdAsync(int permissionId);
        Task<Permission> GetPermissionByNameAsync(string permissionName);
        Task<bool> CreatePermissionAsync(Permission permission);
        Task<bool> UpdatePermissionAsync(Permission permission);
        Task<bool> DeletePermissionAsync(int permissionId);
        Task<IEnumerable<Permission>> GetPermissionsByModuleAsync(string module);
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId);
    }
}
