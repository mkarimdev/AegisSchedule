using Microsoft.EntityFrameworkCore;

namespace AegisSchedule.Api.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(UniSchedulingDbContext context)
    {
        // Ensure the PostgreSQL database schema is created and all migrations applied
        await context.Database.MigrateAsync();
    }
}
