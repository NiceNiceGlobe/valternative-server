using Microsoft.AspNetCore.Identity;
using ValternativeServer.Models.Auth;

namespace ValternativeServer.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ValternativeUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            var roles = new[] { "Admin", "Recruiter" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new Role { Name = role });
                }
            }

            var adminEmail = "admin@valternative.com";
            var recruiterEmail = "recruiter@valternative.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ValternativeUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            if (await userManager.FindByEmailAsync(recruiterEmail) == null)
            {
                var recruiter = new ValternativeUser
                {
                    UserName = recruiterEmail,
                    Email = recruiterEmail,
                    FirstName = "Default",
                    LastName = "Recruiter",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(recruiter, "Recruiter@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(recruiter, "Recruiter");
                }
            }
        }
    }
}