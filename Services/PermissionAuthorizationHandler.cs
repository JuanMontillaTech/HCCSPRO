using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;

namespace ALGASystem.Services
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionService _permissionService;
        private readonly IRoleService _roleService;
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(
            UserManager<ApplicationUser> userManager,
            IPermissionService permissionService,
            IRoleService roleService,
            ILogger<PermissionAuthorizationHandler> logger)
        {
            _userManager = userManager;
            _permissionService = permissionService;
            _roleService = roleService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            try
            {
                _logger.LogInformation($"Checking permission: {requirement.Permission}");
                
                if (context.User == null)
                {
                    _logger.LogWarning("Authorization failed: User is null");
                    return;
                }
                
                if (!context.User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Authorization failed: User is not authenticated");
                    return;
                }

                // Get the user
                var user = await _userManager.GetUserAsync(context.User);
                if (user == null)
                {
                    _logger.LogWarning("Authorization failed: Unable to retrieve user from UserManager");
                    return;
                }
                
                _logger.LogInformation($"Checking permissions for user: {user.UserName} (ID: {user.Id})");

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Authorization failed: User {user.UserName} is not active");
                    return;
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"User has roles: {string.Join(", ", roles)}");
                
                // Check if user has direct permission
                var userPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);
                _logger.LogInformation($"User has {userPermissions.Count()} direct permissions");
                
                if (userPermissions.Any(p => p.Name == requirement.Permission))
                {
                    _logger.LogInformation($"User has direct permission: {requirement.Permission}");
                    context.Succeed(requirement);
                    return;
                }

                // Check if any of user's roles has the permission
                foreach (var roleName in roles)
                {
                    var role = await _roleService.GetRoleByNameAsync(roleName);
                    if (role != null)
                    {
                        var rolePermissions = await _permissionService.GetRolePermissionsAsync(role.Id);
                        _logger.LogInformation($"Role {roleName} has {rolePermissions.Count()} permissions");
                        
                        if (rolePermissions.Any(p => p.Name == requirement.Permission))
                        {
                            _logger.LogInformation($"Role {roleName} has permission: {requirement.Permission}");
                            context.Succeed(requirement);
                            return;
                        }
                    }
                }
                
                _logger.LogWarning($"Authorization failed: User {user.UserName} does not have permission {requirement.Permission}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking permission {requirement.Permission}");
            }
        }
    }
}
