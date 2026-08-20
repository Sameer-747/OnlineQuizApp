using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            const string adminEmail = "admin@quizapp.com";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                // Read the initial super-admin password from configuration (set Seed__AdminPassword
                // as an environment variable in Render) so it's never checked into source control.
                // Falls back to a default only for local/first-run convenience - change it immediately
                // after first login either way, via Profile > Change Password.
                var adminPassword = configuration["Seed:AdminPassword"];
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    adminPassword = "Admin@123";
                    logger.LogWarning(
                        "No Seed:AdminPassword configured - seeding the super admin account with the " +
                        "default password. Set the Seed__AdminPassword environment variable and change " +
                        "this account's password immediately after first login.");
                }

                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
