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
            var userPermissions = await _dbContext.UserPermissions
                .Where(up => up.UserId == userId)
                .Include(up => up.Permission)
                .ToListAsync();

            return userPermissions.Select(up => up.Permission).ToList();
        }

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId)
        {
            var rolePermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .ToListAsync();

            return rolePermissions.Select(rp => rp.Permission).ToList();
        }
    }
}
