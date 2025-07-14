using ALGASystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ALGASystem.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                    // Apply migrations if they are pending
                    context.Database.Migrate();

                    logger.LogInformation("Starting database initialization...");

                    // Seed roles
                    await SeedRoles(roleManager);

                    // Seed admin user
                    await SeedAdminUser(userManager);

                    // Seed permissions
                    await SeedPermissions(context);

                    // Assign permissions to admin role
                    await AssignPermissionsToAdminRole(context);

                    logger.LogInformation("Database initialization completed successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
        {
            string[] roleNames = { "Administrator", "Manager", "User" };
            string[] roleDescriptions = { 
                "Full access to all system features", 
                "Access to management features", 
                "Basic user access" 
            };

            for (int i = 0; i < roleNames.Length; i++)
            {
                var roleName = roleNames[i];
                var roleExists = await roleManager.RoleExistsAsync(roleName);

                if (!roleExists)
                {
                    var role = new ApplicationRole
                    {
                        Name = roleName,
                        Description = roleDescriptions[i]
                    };

                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@algasystem.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }
        }

        private static async Task SeedPermissions(ApplicationDbContext context)
        {
            // Define permission modules
            string[] modules = { "Users", "Roles", "Permissions", "Dashboard", "Settings" };

            // Define permissions for each module
            var permissionsToAdd = new List<Permission>();

            // User permissions
            permissionsToAdd.Add(new Permission { Name = "View Users", Module = "Users", Description = "Can view user list" });
            permissionsToAdd.Add(new Permission { Name = "Create Users", Module = "Users", Description = "Can create new users" });
            permissionsToAdd.Add(new Permission { Name = "Edit Users", Module = "Users", Description = "Can edit existing users" });
            permissionsToAdd.Add(new Permission { Name = "Delete Users", Module = "Users", Description = "Can delete users" });
            permissionsToAdd.Add(new Permission { Name = "Manage User Roles", Module = "Users", Description = "Can assign roles to users" });
            permissionsToAdd.Add(new Permission { Name = "Manage User Permissions", Module = "Users", Description = "Can assign direct permissions to users" });

            // Role permissions
            permissionsToAdd.Add(new Permission { Name = "View Roles", Module = "Roles", Description = "Can view role list" });
            permissionsToAdd.Add(new Permission { Name = "Create Roles", Module = "Roles", Description = "Can create new roles" });
            permissionsToAdd.Add(new Permission { Name = "Edit Roles", Module = "Roles", Description = "Can edit existing roles" });
            permissionsToAdd.Add(new Permission { Name = "Delete Roles", Module = "Roles", Description = "Can delete roles" });
            permissionsToAdd.Add(new Permission { Name = "Manage Role Permissions", Module = "Roles", Description = "Can assign permissions to roles" });

            // Permission permissions
            permissionsToAdd.Add(new Permission { Name = "View Permissions", Module = "Permissions", Description = "Can view permission list" });
            permissionsToAdd.Add(new Permission { Name = "Create Permissions", Module = "Permissions", Description = "Can create new permissions" });
            permissionsToAdd.Add(new Permission { Name = "Edit Permissions", Module = "Permissions", Description = "Can edit existing permissions" });
            permissionsToAdd.Add(new Permission { Name = "Delete Permissions", Module = "Permissions", Description = "Can delete permissions" });

            // Dashboard permissions
            permissionsToAdd.Add(new Permission { Name = "View Dashboard", Module = "Dashboard", Description = "Can view dashboard" });
            permissionsToAdd.Add(new Permission { Name = "View Statistics", Module = "Dashboard", Description = "Can view statistics on dashboard" });

            // Settings permissions
            permissionsToAdd.Add(new Permission { Name = "View Settings", Module = "Settings", Description = "Can view application settings" });
            permissionsToAdd.Add(new Permission { Name = "Edit Settings", Module = "Settings", Description = "Can edit application settings" });
            permissionsToAdd.Add(new Permission { Name = "Manage Backup", Module = "Settings", Description = "Can manage database backups" });

            foreach (var permission in permissionsToAdd)
            {
                var existingPermission = await context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permission.Name && p.Module == permission.Module);

                if (existingPermission == null)
                {
                    context.Permissions.Add(permission);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task AssignPermissionsToAdminRole(ApplicationDbContext context)
        {
            // Get admin role
            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Administrator");

            if (adminRole == null)
            {
                Console.WriteLine("ERROR: Admin role not found during permission assignment");
                return;
            }

            // Get all permissions
            var allPermissions = await context.Permissions.ToListAsync();
            
            // Ensure View Dashboard permission exists
            var viewDashboardPermission = allPermissions.FirstOrDefault(p => p.Name == "View Dashboard");
            if (viewDashboardPermission == null)
            {
                Console.WriteLine("ERROR: View Dashboard permission not found");
                // If not found, create it
                viewDashboardPermission = new Permission
                {
                    Name = "View Dashboard",
                    Module = "Dashboard",
                    Description = "Can view dashboard"
                };
                context.Permissions.Add(viewDashboardPermission);
                await context.SaveChangesAsync();
                allPermissions = await context.Permissions.ToListAsync(); // Refresh permissions list
            }
            else
            {
                Console.WriteLine($"View Dashboard permission found with ID: {viewDashboardPermission.Id}");
            }

            // Check if admin role already has all permissions
            var existingRolePermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == adminRole.Id)
                .ToListAsync();

            // Add missing permissions to admin role
            foreach (var permission in allPermissions)
            {
                if (!existingRolePermissions.Any(rp => rp.PermissionId == permission.Id))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                    Console.WriteLine($"Added permission {permission.Name} to Administrator role");
                }
            }

            await context.SaveChangesAsync();
            
            // Verify the View Dashboard permission is assigned
            var dashboardPermission = allPermissions.FirstOrDefault(p => p.Name == "View Dashboard");
            if (dashboardPermission != null)
            {
                var hasPermission = await context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == dashboardPermission.Id);
                Console.WriteLine($"Administrator role has View Dashboard permission: {hasPermission}");
            }
        }
    }
}
