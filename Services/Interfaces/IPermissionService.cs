using ALGASystem.Models;
using ALGASystem.Services;
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
        
        /// <summary>
        /// Obtiene todos los datos de autorización del usuario en una sola operación
        /// para evitar problemas de concurrencia
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="roles">Roles del usuario</param>
        /// <returns>Datos de autorización completos</returns>
        Task<UserAuthorizationData> GetUserAuthorizationDataAsync(string userId, IEnumerable<string> roles);
    }
}
