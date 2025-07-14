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
                _logger.LogInformation($"Verificando permiso: {requirement.Permission}");
                
                if (context.User == null)
                {
                    _logger.LogWarning("Autorización fallida: El usuario es nulo");
                    return;
                }
                
                if (!context.User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Autorización fallida: El usuario no está autenticado");
                    return;
                }

                // Get the user
                var user = await _userManager.GetUserAsync(context.User);
                if (user == null)
                {
                    _logger.LogWarning("Autorización fallida: No se pudo recuperar el usuario desde UserManager");
                    return;
                }
                
                _logger.LogInformation($"Verificando permisos para el usuario: {user.UserName} (ID: {user.Id})");

                // Check if user is active
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Autorización fallida: El usuario {user.UserName} no está activo");
                    return;
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation($"El usuario tiene los roles: {string.Join(", ", roles)}");
                
                // Check if user has direct permission
                var userPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);
                _logger.LogInformation($"El usuario tiene {userPermissions.Count()} permisos directos");
                
                if (userPermissions.Any(p => p.Name == requirement.Permission))
                {
                    _logger.LogInformation($"El usuario tiene permiso directo: {requirement.Permission}");
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
                        _logger.LogInformation($"El rol {roleName} tiene {rolePermissions.Count()} permisos");
                        
                        if (rolePermissions.Any(p => p.Name == requirement.Permission))
                        {
                            _logger.LogInformation($"El rol {roleName} tiene permiso: {requirement.Permission}");
                            context.Succeed(requirement);
                            return;
                        }
                    }
                }
                
                _logger.LogWarning($"Autorización fallida: El usuario {user.UserName} no tiene el permiso {requirement.Permission}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar el permiso {requirement.Permission}");
            }
        }
    }
}
