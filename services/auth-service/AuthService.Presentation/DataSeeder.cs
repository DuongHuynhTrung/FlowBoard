using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;

namespace AuthService.Presentation
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            // Apply migrations dynamically if they exist (good practice for quick setup)
            if (context.Database.IsSqlServer())
            {
                await context.Database.MigrateAsync();
            }

            if (!await context.Roles.AnyAsync())
            {
                var adminRole = new Role { Id = Guid.NewGuid(), Name = RoleEnum.Admin.ToString(), Description = "Full access to FlowBoard" };
                var memberRole = new Role { Id = Guid.NewGuid(), Name = RoleEnum.Member.ToString(), Description = "Create projects and workspaces" };
                var viewerRole = new Role { Id = Guid.NewGuid(), Name = RoleEnum.Viewer.ToString(), Description = "Read-only access" };

                await context.Roles.AddRangeAsync(adminRole, memberRole, viewerRole);
                await context.SaveChangesAsync();
            }
        }
    }
}
