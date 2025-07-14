using ALGASystem.Data.Interfaces;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ALGASystem.Data;

namespace ALGASystem.Services
{
    // Clase que contiene todos los datos de permisos para el usuario
    // para evitar operaciones concurrentes de Entity Framework
    public class UserAuthorizationData
    {
        public IEnumerable<Permission> UserPermissions { get; set; }
        public Dictionary<string, IEnumerable<Permission>> RolePermissions { get; set; }
        
        public UserAuthorizationData()
        {
            UserPermissions = new List<Permission>();
            RolePermissions = new Dictionary<string, IEnumerable<Permission>>();
        }
        
        public bool HasPermission(string permissionName)
        {
            // Verificar si el usuario tiene el permiso directamente
            if (UserPermissions.Any(p => p.Name == permissionName))
                return true;
                
            // Verificar si alguno de los roles del usuario tiene el permiso
            foreach (var rolePermissions in RolePermissions.Values)
            {
                if (rolePermissions.Any(p => p.Name == permissionName))
                    return true;
            }
            
            return false;
        }
    }

    public class PermissionService : IPermissionService
    {
        private readonly IGenericRepository<Permission> _permissionRepository;
        private readonly IGenericRepository<UserPermission> _userPermissionRepository;
        private readonly IGenericRepository<RolePermission> _rolePermissionRepository;
        private readonly ApplicationDbContext _dbContext;

        public PermissionService(
            IGenericRepository<Permission> permissionRepository,
            IGenericRepository<UserPermission> userPermissionRepository,
            IGenericRepository<RolePermission> rolePermissionRepository,
            ApplicationDbContext dbContext)
        {
            _permissionRepository = permissionRepository;
            _userPermissionRepository = userPermissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            return await _permissionRepository.GetAllAsync();
        }

        public async Task<Permission> GetPermissionByIdAsync(int permissionId)
        {
            return await _permissionRepository.GetByIdAsync(permissionId);
        }

        public async Task<Permission> GetPermissionByNameAsync(string permissionName)
        {
            return await _permissionRepository.SingleOrDefaultAsync(p => p.Name == permissionName);
        }

        public async Task<bool> CreatePermissionAsync(Permission permission)
        {
            await _permissionRepository.AddAsync(permission);
            return await _permissionRepository.SaveChangesAsync();
        }

        public async Task<bool> UpdatePermissionAsync(Permission permission)
        {
            _permissionRepository.Update(permission);
            return await _permissionRepository.SaveChangesAsync();
        }

        public async Task<bool> DeletePermissionAsync(int permissionId)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId);
            if (permission == null)
            {
                return false;
            }
            _permissionRepository.Remove(permission);
            return await _permissionRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByModuleAsync(string module)
        {
            return await _permissionRepository.FindAsync(p => p.Module == module);
        }

        public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId)
        {
            // MEJORA: Materializamos la consulta completa inmediatamente para evitar consultas posteriores en el mismo contexto
            // que podrían causar problemas de concurrencia
            var userPermissions = await _dbContext.UserPermissions
                .AsNoTracking() // Reduce el seguimiento de entidades para mejorar el rendimiento
                .Where(up => up.UserId == userId)
                .Include(up => up.Permission)
                .ToListAsync();

            // Extraemos y materializamos los permisos en memoria
            var permissions = userPermissions.Select(up => up.Permission).ToList();
            
            return permissions;
        }

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId)
        {
            // MEJORA: Materializamos la consulta completa inmediatamente para evitar consultas posteriores en el mismo contexto
            // que podrían causar problemas de concurrencia
            var rolePermissions = await _dbContext.RolePermissions
                .AsNoTracking() // Reduce el seguimiento de entidades para mejorar el rendimiento
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .ToListAsync();

            // Extraemos y materializamos los permisos en memoria
            var permissions = rolePermissions.Select(rp => rp.Permission).ToList();
        
            return permissions;
        }
        
        /// <summary>
        /// Obtiene todos los datos de autorización del usuario en una sola operación
        /// para evitar problemas de concurrencia
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="roles">Roles del usuario</param>
        /// <returns>Datos de autorización completos</returns>
        public async Task<UserAuthorizationData> GetUserAuthorizationDataAsync(string userId, IEnumerable<string> roles)
        {
            var result = new UserAuthorizationData();
            
            // Obtenemos los permisos directos del usuario
            var userPermissions = await _dbContext.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Include(up => up.Permission)
                .ToListAsync();
            
            result.UserPermissions = userPermissions.Select(up => up.Permission).ToList();
            
            // Obtenemos todos los roles y sus permisos
            foreach (var roleName in roles)
            {
                // Usar la tabla Roles que es la estándar de Identity
                var role = await _dbContext.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Name == roleName);
                    
                if (role != null)
                {
                    var rolePermissions = await _dbContext.RolePermissions
                        .AsNoTracking()
                        .Where(rp => rp.RoleId == role.Id)
                        .Include(rp => rp.Permission)
                        .ToListAsync();
                        
                    result.RolePermissions[roleName] = rolePermissions.Select(rp => rp.Permission).ToList();
                }
            }
            
            return result;
        }
    }
}
