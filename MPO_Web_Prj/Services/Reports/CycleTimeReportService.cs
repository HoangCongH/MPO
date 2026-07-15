using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class CycleTimeReportService : ICycleTimeReportService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AppDbContext dbContext;

    public CycleTimeReportService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<CycleTimeReportViewModel> GetReportAsync(CycleTimeReportFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);

        try
        {
            var machines = await dbContext.master_machines
                .AsNoTracking()
                .Select(machine => new MachineSelection
                {
                    Id = machine.id,
                    Line = machine.line,
                    MachineName = machine.machine_name,
                    Stage = machine.stage
                })
                .ToListAsync(cancellationToken);

            var lineOptions = BuildOptions(machines
                .Select(machine => machine.Line)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct()
                .OrderBy(line => line!)
                .Select(line => line!));

            if (!filter.IsApplied)
            {
                return new CycleTimeReportViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    ModelOptions = BuildOptions([]),
                    Rows = []
                };
            }

            var selectedMachineIds = GetLastMachineIdsByLine(machines, filter.LineName);

            var latestReportQuery = dbContext.production_reports
                .AsNoTracking()
                .AsQueryable();

            if (selectedMachineIds.Count > 0)
            {
                latestReportQuery = latestReportQuery.Where(report => report.machine_id != null
                    && selectedMachineIds.Contains(report.machine_id));
            }

            var latestReportDate = await latestReportQuery
                .Where(report => report.report_date != null)
                .OrderByDescending(report => report.report_date)
                .Select(report => report.report_date)
                .FirstOrDefaultAsync(cancellationToken);

            SetDefaultDateRange(filter, latestReportDate);

            if (!lineOptions.Any(option => option.Value == filter.LineName))
            {
                filter.LineName = null;
                selectedMachineIds = GetLastMachineIdsByLine(machines, filter.LineName);
            }

            var modelOptionQuery = dbContext.production_reports
                .AsNoTracking()
                .Where(report => report.lot_name != null && report.lot_name != string.Empty);

            if (selectedMachineIds.Count > 0)
            {
                modelOptionQuery = modelOptionQuery.Where(report => report.machine_id != null
                    && selectedMachineIds.Contains(report.machine_id));
            }

            var modelOptions = BuildOptions(await modelOptionQuery
                .Select(report => report.lot_name!)
                .Distinct()
                .OrderBy(lotName => lotName)
                .ToListAsync(cancellationToken));

            if (!modelOptions.Any(option => option.Value == filter.ModelName))
            {
                filter.ModelName = null;
            }

            var query = dbContext.production_reports
                .AsNoTracking()
                .Include(report => report.machine)
                .AsQueryable();

            if (selectedMachineIds.Count > 0)
            {
                query = query.Where(report => report.machine_id != null
                    && selectedMachineIds.Contains(report.machine_id));
            }

            if (!string.IsNullOrWhiteSpace(filter.ModelName))
            {
                query = query.Where(report => report.lot_name == filter.ModelName);
            }

            if (filter.StartDate.HasValue)
            {
                var startDateTime = filter.StartDate.Value.ToDateTime(filter.StartTime ?? StartOfDay);
                query = query.Where(report => report.report_date >= startDateTime);
            }

            if (filter.EndDate.HasValue)
            {
                if (filter.EndTime.HasValue && filter.EndTime.Value != EndOfDay)
                {
                    var endDateTime = filter.EndDate.Value.ToDateTime(filter.EndTime.Value);
                    query = query.Where(report => report.report_date <= endDateTime);
                }
                else
                {
                    var nextDateTime = filter.EndDate.Value.AddDays(1).ToDateTime(StartOfDay);
                    query = query.Where(report => report.report_date < nextDateTime);
                }
            }

            var rows = await query
                .OrderBy(report => report.report_date)
                .ThenBy(report => report.machine_id)
                .Select(report => new CycleTimeReportRow
                {
                    LineName = report.machine != null && report.machine.line != null
                        ? report.machine.line
                        : string.Empty,
                    ModelName = report.lot_name ?? string.Empty,
                    GroupName = report.mjs_id ?? string.Empty,
                    CycleTime1 = report.cycle_time_1 ?? 0,
                    CycleTime2 = report.cycle_time_2 ?? 0,
                    CycleTime3 = report.cycle_time_3 ?? 0
                })
                .ToListAsync(cancellationToken);

            return new CycleTimeReportViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                ModelOptions = modelOptions,
                Rows = rows
            };
        }
        catch (NpgsqlException ex)
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
        catch (TimeoutException ex)
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
        catch (InvalidOperationException ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    private static CycleTimeReportViewModel CreateDatabaseErrorViewModel(CycleTimeReportFilter filter, Exception exception)
    {
        return new CycleTimeReportViewModel
        {
            Filter = filter,
            ErrorMessage = $"Cannot connect to PostgreSQL database. Please check the DB server/IP, network/VPN, port 5432, database name, username and password. Detail: {exception.Message}"
        };
    }

    private static List<ReportSelectOption> BuildOptions(IEnumerable<string> values)
    {
        var options = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        options.AddRange(values.Select(value => new ReportSelectOption
        {
            Value = value,
            Text = value
        }));

        return options;
    }

    private static List<string> GetLastMachineIdsByLine(IReadOnlyList<MachineSelection> machines, string? lineName)
    {
        var query = machines
            .Where(machine => !string.IsNullOrWhiteSpace(machine.Id)
                && !string.IsNullOrWhiteSpace(machine.Line));

        if (!string.IsNullOrWhiteSpace(lineName))
        {
            query = query.Where(machine => machine.Line == lineName);
        }

        return query
            .GroupBy(machine => machine.Line!)
            .Select(group =>
            {
                var maxStage = group.Max(machine => machine.Stage ?? short.MinValue);

                return group
                    .Where(machine => (machine.Stage ?? short.MinValue) == maxStage)
                    .OrderByDescending(machine => GetMachineNumber(machine.MachineName))
                    .ThenByDescending(machine => machine.MachineName)
                    .ThenByDescending(machine => machine.Id)
                    .First()
                    .Id;
            })
            .ToList();
    }

    private static int GetMachineNumber(string? machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName))
        {
            return int.MinValue;
        }

        var end = machineName.Length - 1;
        while (end >= 0 && !char.IsDigit(machineName[end]))
        {
            end--;
        }

        if (end < 0)
        {
            return int.MinValue;
        }

        var start = end;
        while (start >= 0 && char.IsDigit(machineName[start]))
        {
            start--;
        }

        return int.TryParse(machineName.Substring(start + 1, end - start), out var number)
            ? number
            : int.MinValue;
    }

    private static bool IsDatabaseConnectionFailure(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is NpgsqlException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }

    private static void NormalizeFilter(CycleTimeReportFilter filter)
    {
        filter.LineName = string.IsNullOrWhiteSpace(filter.LineName)
            ? null
            : filter.LineName.Trim();

        filter.ModelName = string.IsNullOrWhiteSpace(filter.ModelName)
            ? null
            : filter.ModelName.Trim();
    }

    private static void SetDefaultDateRange(CycleTimeReportFilter filter, DateTime? latestReportDate)
    {
        if (filter.StartDate.HasValue || filter.EndDate.HasValue || !latestReportDate.HasValue)
        {
            return;
        }

        var latestDate = DateOnly.FromDateTime(latestReportDate.Value);
        filter.StartDate = latestDate;
        filter.StartTime = StartOfDay;
        filter.EndDate = latestDate;
        filter.EndTime = EndOfDay;
    }

    private sealed class MachineSelection
    {
        public string Id { get; set; } = string.Empty;

        public string? Line { get; set; }

        public string? MachineName { get; set; }

        public short? Stage { get; set; }
    }
}
