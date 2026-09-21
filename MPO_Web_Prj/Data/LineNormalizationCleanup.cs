using Microsoft.EntityFrameworkCore;

namespace MPO_Web_Prj.Data;

internal static class LineNormalizationCleanup
{
    internal static async Task RemoveLegacyTriggerAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS trg_master_machines_normalize_line ON master_machines;");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not remove the old line normalization trigger. New machine line names may still be changed by the database.");
        }
    }
}
