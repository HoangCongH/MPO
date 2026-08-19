using Microsoft.EntityFrameworkCore;

namespace MPO_Web_Prj.Data;

internal static class LineDataConsolidation
{
    internal static async Task ApplyAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));

        try
        {
            var updated = await dbContext.Database.ExecuteSqlRawAsync("""
                UPDATE master_machines
                SET line = '20'
                WHERE regexp_replace(lower(btrim(COALESCE(line, ''))), '[^a-z0-9]', '', 'g')
                      IN ('line1', '1', 'line20', '20')
                  AND line IS DISTINCT FROM '20';
                """);

            if (updated > 0)
            {
                logger.LogInformation("Consolidated {MachineCount} Line 1/20 machine mappings into Line 20.", updated);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not consolidate existing Line 1/20 master data. Report queries will use canonical line mapping as a fallback.");
        }

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                CREATE OR REPLACE FUNCTION mpo_canonical_line_name(input_line text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                AS $$
                    SELECT CASE
                        WHEN regexp_replace(lower(btrim(COALESCE(input_line, ''))), '[^a-z0-9]', '', 'g')
                             IN ('line1', '1', 'line20', '20')
                            THEN '20'
                        ELSE NULLIF(btrim(input_line), '')
                    END;
                $$;

                CREATE OR REPLACE FUNCTION mpo_normalize_master_machine_line()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    NEW.line := mpo_canonical_line_name(NEW.line);
                    RETURN NEW;
                END;
                $$;

                DROP TRIGGER IF EXISTS trg_master_machines_normalize_line ON master_machines;
                CREATE TRIGGER trg_master_machines_normalize_line
                BEFORE INSERT OR UPDATE OF line ON master_machines
                FOR EACH ROW
                EXECUTE FUNCTION mpo_normalize_master_machine_line();
                """);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not install the Line 1/20 normalization trigger. Existing data and Downtime Report queries remain normalized.");
        }
    }
}
