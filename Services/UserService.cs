using ALGASystem.Data;
using ALGASystem.Data.Interfaces;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ALGASystem.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGenericRepository<UserPermission> _userPermissionRepository;
        private readonly ApplicationDbContext _context;

        public UserService(
            UserManager<ApplicationUser> userManager,
            IGenericRepository<UserPermission> userPermissionRepository,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userPermissionRepository = userPermissionRepository;
            _context = context;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<ApplicationUser> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
        {
            return await _userManager.CreateAsync(user, password);
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }
            return await _userManager.DeleteAsync(user);
        }

        public async Task<IdentityResult> AddUserToRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }
            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> RemoveUserFromRoleAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }
            return await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new List<string>();
            }
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> HasPermissionAsync(string userId, string permissionName)
        {
            // Check direct user permissions
            var directPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId && up.Permission.Name == permissionName)
                .AnyAsync();

            if (directPermission)
            {
                return true;
            }

            // Check role-based permissions
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            
            foreach (var roleName in userRoles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    var hasPermission = await _context.RolePermissions
                        .Include(rp => rp.Permission)
                        .Where(rp => rp.RoleId == role.Id && rp.Permission.Name == permissionName)
                        .AnyAsync();

                    if (hasPermission)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<IdentityResult> AssignPermissionToUserAsync(string userId, int permissionId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Permiso no encontrado" });
            }

            var existingPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            if (existingPermission != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Permiso ya asignado al usuario" });
            }

            var userPermission = new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId
            };

            await _userPermissionRepository.AddAsync(userPermission);
            await _userPermissionRepository.SaveChangesAsync();

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RemovePermissionFromUserAsync(string userId, int permissionId)
        {
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            if (userPermission == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "El usuario no tiene este permiso" });
            }

            _userPermissionRepository.Remove(userPermission);
            await _userPermissionRepository.SaveChangesAsync();

            return IdentityResult.Success;
        }

        public async Task<IEnumerable<Permission>> GetEffectivePermissionsAsync(string userId)
        {
            var result = new List<Permission>();

            // Get direct user permissions
            var directPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Include(up => up.Permission)
                .Select(up => up.Permission)
                .ToListAsync();

            result.AddRange(directPermissions);

            // Get role-based permissions
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var roleName in userRoles)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                    if (role != null)
                    {
                        var rolePermissions = await _context.RolePermissions
                            .Where(rp => rp.RoleId == role.Id)
                            .Include(rp => rp.Permission)
                            .Select(rp => rp.Permission)
                            .ToListAsync();

                        result.AddRange(rolePermissions);
                    }
                }
            }

            // Remove duplicates based on permission ID
            return result.GroupBy(p => p.Id).Select(g => g.First()).ToList();
        }

        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        public async Task LogActivityAsync(string userId, string action)
        {
            // Implement activity logging
            // This could be stored in a database table or log file
            // For now, we'll just log to the console
            var user = await _userManager.FindByIdAsync(userId);
            var username = user?.UserName ?? "Unknown";
            
            Console.WriteLine($"[{DateTime.Now}] User {username} ({userId}): {action}");
            
            // In a real implementation, you would save this to a database
            // For example:
            // await _activityLogRepository.AddAsync(new ActivityLog { UserId = userId, Action = action, Timestamp = DateTime.Now });
            // await _activityLogRepository.SaveChangesAsync();
            
            await Task.CompletedTask;
        }
    }
}
