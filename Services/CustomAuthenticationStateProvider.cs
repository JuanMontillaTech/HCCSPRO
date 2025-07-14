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
            _logger.LogInformation("Getting authentication state");
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null");
                return new AuthenticationState(_currentUser);
            }

            if (httpContext.User == null || !httpContext.User.Identity.IsAuthenticated)
            {
                _logger.LogInformation("User is not authenticated via HttpContext");
                
                // Si el usuario no está autenticado en el HttpContext pero tenemos un _currentUser autenticado,
                // usamos ese en su lugar (esto ayuda con la persistencia en Blazor Server)
                if (_currentUser.Identity.IsAuthenticated)
                {
                    _logger.LogInformation("Using cached authenticated user");
                    return new AuthenticationState(_currentUser);
                }
                
                _logger.LogInformation("No authenticated user found");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            _logger.LogInformation($"User is authenticated as: {httpContext.User.Identity.Name}");
            var user = await _userManager.GetUserAsync(httpContext.User);

            if (user == null)
            {
                _logger.LogWarning("User found in HttpContext but not in UserManager");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Verificar si el usuario está activo
            if (!user.IsActive)
            {
                _logger.LogWarning($"User {user.UserName} is not active");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var principal = new ClaimsPrincipal(httpContext.User);
            _currentUser = principal; // Actualizar el usuario actual en caché
            return new AuthenticationState(principal);
        }

        public void NotifyAuthenticationStateChangedExternally()
        {
            _logger.LogInformation("Authentication state changed externally");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation($"Validating user: {username}");
                
                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning($"User {username} not found");
                    return false;
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"User {username} is not active");
                    return false;
                }

                // En lugar de usar SignInManager, verificamos la contraseña directamente
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                
                if (isPasswordValid)
                {
                    _logger.LogInformation($"User {username} authenticated successfully");
                    
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
                
                _logger.LogWarning($"Invalid password for user {username}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating user {username}");
                return false;
            }
        }

        public void MarkUserAsAuthenticated(string userName)
        {
            _logger.LogInformation($"Marking user as authenticated: {userName}");
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userName)
            }, "Custom");
            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void MarkUserAsLoggedOut()
        {
            _logger.LogInformation("Marking user as logged out");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}

