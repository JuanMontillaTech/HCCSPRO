using ALGASystem.Data;
using ALGASystem.Data.Interfaces;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ALGASystem.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IGenericRepository<RolePermission> _rolePermissionRepository;
        private readonly ApplicationDbContext _context;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            IGenericRepository<RolePermission> rolePermissionRepository,
            ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _rolePermissionRepository = rolePermissionRepository;
            _context = context;
        }

        public async Task<IEnumerable<ApplicationRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        public async Task<ApplicationRole> GetRoleByIdAsync(string roleId)
        {
            return await _roleManager.FindByIdAsync(roleId);
        }

        public async Task<ApplicationRole> GetRoleByNameAsync(string roleName)
        {
            return await _roleManager.FindByNameAsync(roleName);
        }

        public async Task<IdentityResult> CreateRoleAsync(ApplicationRole role)
        {
            return await _roleManager.CreateAsync(role);
        }

        public async Task<IdentityResult> UpdateRoleAsync(ApplicationRole role)
        {
            return await _roleManager.UpdateAsync(role);
        }

        public async Task<IdentityResult> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Role not found" });
            }
            return await _roleManager.DeleteAsync(role);
        }

        public async Task<IdentityResult> AssignPermissionToRoleAsync(string roleId, int permissionId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Role not found" });
            }

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Permission not found" });
            }

            var existingPermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existingPermission != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Permission already assigned to role" });
            }

            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            };

            await _rolePermissionRepository.AddAsync(rolePermission);
            await _rolePermissionRepository.SaveChangesAsync();

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RemovePermissionFromRoleAsync(string roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Role does not have this permission" });
            }

            _rolePermissionRepository.Remove(rolePermission);
            await _rolePermissionRepository.SaveChangesAsync();

            return IdentityResult.Success;
        }

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync();
        }
    }
}
