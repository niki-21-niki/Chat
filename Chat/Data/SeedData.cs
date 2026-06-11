using Chat.Data;
using Microsoft.AspNetCore.Identity;

namespace Chat.Data
{ 
    public static class SeedData
    {
        public static async Task Initialize(
            IServiceProvider serviceProvider,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            using (var context = serviceProvider.GetRequiredService<AppDbContext>())
            {
                // Ваша логика инициализации данных
                await SeedRoles(roleManager);
                await SeedUsers(userManager);
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }
        }

        private static async Task SeedUsers(UserManager<User> userManager)
        {
            if (await userManager.FindByNameAsync("admin") == null)
            {
                var admin = new User
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    IsOnline = false
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}