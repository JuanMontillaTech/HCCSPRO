using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ALGASystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ALGASystem.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<CustomAuthenticationStateProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            _logger.LogInformation("Obteniendo estado de autenticación");
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("El HttpContext es nulo");
                return new AuthenticationState(_currentUser);
            }

            if (httpContext.User == null || !httpContext.User.Identity.IsAuthenticated)
            {
                _logger.LogInformation("El usuario no está autenticado a través del HttpContext");
                
                // Si el usuario no está autenticado en el HttpContext pero tenemos un _currentUser autenticado,
                // usamos ese en su lugar (esto ayuda con la persistencia en Blazor Server)
                if (_currentUser.Identity.IsAuthenticated)
                {
                    _logger.LogInformation("Usando usuario autenticado en caché");
                    return new AuthenticationState(_currentUser);
                }
                
                _logger.LogInformation("No se encontró usuario autenticado");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            _logger.LogInformation($"Usuario autenticado como: {httpContext.User.Identity.Name}");
            var user = await _userManager.GetUserAsync(httpContext.User);

            if (user == null)
            {
                _logger.LogWarning("Usuario encontrado en HttpContext pero no en UserManager");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Verificar si el usuario está activo
            if (!user.IsActive)
            {
                _logger.LogWarning($"El usuario {user.UserName} no está activo");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var principal = new ClaimsPrincipal(httpContext.User);
            _currentUser = principal; // Actualizar el usuario actual en caché
            return new AuthenticationState(principal);
        }

        public void NotifyAuthenticationStateChangedExternally()
        {
            _logger.LogInformation("El estado de autenticación ha cambiado externamente");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation($"Validando usuario: {username}");
                
                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning($"Usuario {username} no encontrado");
                    return false;
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"El usuario {username} no está activo");
                    return false;
                }

                // En lugar de usar SignInManager, verificamos la contraseña directamente
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                
                if (isPasswordValid)
                {
                    _logger.LogInformation($"Usuario {username} autenticado exitosamente");
                    
                    // Crear claims para el usuario
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    };
                    
                    // Agregar claims personalizados si existen
                    var userClaims = await _userManager.GetClaimsAsync(user);
                    claims.AddRange(userClaims);
                    
                    // Agregar roles como claims
                    var userRoles = await _userManager.GetRolesAsync(user);
                    foreach (var role in userRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                    
                    // Crear la identidad y el principal
                    var identity = new ClaimsIdentity(claims, "Custom");
                    _currentUser = new ClaimsPrincipal(identity);
                    
                    // Notificar el cambio de estado de autenticación
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                    return true;
                }
                
                _logger.LogWarning($"Contraseña inválida para el usuario {username}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al validar el usuario {username}");
                return false;
            }
        }

        public void MarkUserAsAuthenticated(string userName)
        {
            _logger.LogInformation($"Marcando al usuario como autenticado: {userName}");
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userName)
            }, "Custom");
            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void MarkUserAsLoggedOut()
        {
            _logger.LogInformation("Marcando al usuario como desconectado");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}

