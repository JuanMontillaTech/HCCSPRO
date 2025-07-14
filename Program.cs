using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using ALGASystem.Data;
using ALGASystem.Models;
using ALGASystem.Data.Interfaces;
using ALGASystem.Services;
using ALGASystem.Services.Interfaces;
using ALGASystem.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Razor & Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity con ApplicationUser y ApplicationRole
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configuración del esquema de cookies para Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/accessdenied";
    options.SlidingExpiration = true;
});

// IHttpContextAccessor requerido por SignInManager
builder.Services.AddHttpContextAccessor();

// Estado de autenticación personalizado
builder.Services.AddScoped<ALGASystem.Services.CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<ALGASystem.Services.CustomAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Repositorios y servicios
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Handler para políticas por permiso
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Políticas de autorización personalizadas
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ViewUsers", policy => policy.Requirements.Add(new PermissionRequirement("View Users")));
    options.AddPolicy("CreateUsers", policy => policy.Requirements.Add(new PermissionRequirement("Create Users")));
    options.AddPolicy("EditUsers", policy => policy.Requirements.Add(new PermissionRequirement("Edit Users")));
    options.AddPolicy("DeleteUsers", policy => policy.Requirements.Add(new PermissionRequirement("Delete Users")));
    options.AddPolicy("ManageUserRoles", policy => policy.Requirements.Add(new PermissionRequirement("Manage User Roles")));
    options.AddPolicy("ManageUserPermissions", policy => policy.Requirements.Add(new PermissionRequirement("Manage User Permissions")));

    options.AddPolicy("ViewRoles", policy => policy.Requirements.Add(new PermissionRequirement("View Roles")));
    options.AddPolicy("CreateRoles", policy => policy.Requirements.Add(new PermissionRequirement("Create Roles")));
    options.AddPolicy("EditRoles", policy => policy.Requirements.Add(new PermissionRequirement("Edit Roles")));
    options.AddPolicy("DeleteRoles", policy => policy.Requirements.Add(new PermissionRequirement("Delete Roles")));
    options.AddPolicy("ManageRolePermissions", policy => policy.Requirements.Add(new PermissionRequirement("Manage Role Permissions")));

    options.AddPolicy("ViewPermissions", policy => policy.Requirements.Add(new PermissionRequirement("View Permissions")));
    options.AddPolicy("CreatePermissions", policy => policy.Requirements.Add(new PermissionRequirement("Create Permissions")));
    options.AddPolicy("EditPermissions", policy => policy.Requirements.Add(new PermissionRequirement("Edit Permissions")));
    options.AddPolicy("DeletePermissions", policy => policy.Requirements.Add(new PermissionRequirement("Delete Permissions")));

    options.AddPolicy("ViewDashboard", policy => policy.Requirements.Add(new PermissionRequirement("View Dashboard")));
    options.AddPolicy("ViewStatistics", policy => policy.Requirements.Add(new PermissionRequirement("View Statistics")));

    options.AddPolicy("ViewSettings", policy => policy.Requirements.Add(new PermissionRequirement("View Settings")));
    options.AddPolicy("EditSettings", policy => policy.Requirements.Add(new PermissionRequirement("Edit Settings")));
    options.AddPolicy("ManageBackup", policy => policy.Requirements.Add(new PermissionRequirement("Manage Backup")));
});

// HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Blazor Hub y fallback
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Inicialización de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Initializing database...");
        await DbInitializer.Initialize(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
