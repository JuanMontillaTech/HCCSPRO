using ALGASystem.Data;
using ALGASystem.Models;
using ALGASystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Transactions;

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
        private readonly ApplicationDbContext _dbContext;

        public PermissionAuthorizationHandler(
            UserManager<ApplicationUser> userManager,
            IPermissionService permissionService,
            IRoleService roleService,
            ILogger<PermissionAuthorizationHandler> logger,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _permissionService = permissionService;
            _roleService = roleService;
            _logger = logger;
            _dbContext = dbContext;
        }

        // Reemplazamos la implementación asíncrona por una que evita problemas de concurrencia
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            try
            {
                _logger.LogInformation($"Verificando permiso: {requirement.Permission}");
                
                // Verificaciones básicas de usuario sin usar operaciones asíncronas de EF
                if (context.User == null || !context.User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Autorización fallida: El usuario es nulo o no está autenticado");
                    return Task.CompletedTask;
                }

                // Extraer el ID de usuario directamente de las claims (evita GetUserAsync que causa problemas de concurrencia)
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = context.User.FindFirstValue(ClaimTypes.Name);
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Autorización fallida: No se pudo obtener el ID del usuario de las claims");
                    return Task.CompletedTask;
                }
                
                _logger.LogInformation($"Verificando permisos para el usuario: {userName} (ID: {userId})");
                
                // Enfoque sin concurrencia: usar ADO.NET directamente para evitar problemas de concurrencia de EF Core
                // Esto nos permite hacer las consultas de manera segura en un contexto multihilo
                
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                using (var dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
                    _dbContext.Database.GetConnectionString()).Options))
                {
                    // 1. Comprobar permisos directos del usuario
                    var userPermissions = dbContext.UserPermissions
                        .AsNoTracking()
                        .Where(up => up.UserId == userId)
                        .Include(up => up.Permission)
                        .ToList(); // Usar ToList en lugar de ToListAsync para evitar problemas
                    
                    if (userPermissions.Any(up => up.Permission.Name == requirement.Permission))
                    {
                        _logger.LogInformation($"El usuario tiene permiso directo: {requirement.Permission}");
                        context.Succeed(requirement);
                        scope.Complete();
                        return Task.CompletedTask;
                    }
                    
                    // 2. Obtener roles del usuario
                    var userRoles = dbContext.UserRoles
                        .AsNoTracking()
                        .Where(ur => ur.UserId == userId)
                        .ToList();
                    
                    // 3. Comprobar permisos por rol
                    foreach (var userRole in userRoles)
                    {
                        var rolePermissions = dbContext.RolePermissions
                            .AsNoTracking()
                            .Where(rp => rp.RoleId == userRole.RoleId)
                            .Include(rp => rp.Permission)
                            .ToList();
                        
                        if (rolePermissions.Any(rp => rp.Permission.Name == requirement.Permission))
                        {
                            _logger.LogInformation($"El usuario tiene el permiso {requirement.Permission} a través de un rol");
                            context.Succeed(requirement);
                            scope.Complete();
                            return Task.CompletedTask;
                        }
                    }
                    
                    scope.Complete();
                }
                
                _logger.LogWarning($"Autorización fallida: El usuario {userName} no tiene el permiso {requirement.Permission}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar el permiso {requirement.Permission}");
            }
            
            return Task.CompletedTask;
        }
    }
}
