using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class ProductionReportService : IProductionReportService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AppDbContext dbContext;

    public ProductionReportService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ProductionReportViewModel> GetReportAsync(ProductionReportFilter filter, CancellationToken cancellationToken)
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
            return new ProductionReportViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                ModelOptions = BuildOptions([]),
                Rows = []
            };
        }

            var latestReportQuery = dbContext.production_reports
                .AsNoTracking()
                .Include(report => report.machine)
                .AsQueryable();

            var selectedMachineIds = GetLastMachineIdsByLine(machines, filter.LineName);

            if (selectedMachineIds.Count > 0)
            {
                latestReportQuery = latestReportQuery.Where(report => report.machine_id != null
                    && selectedMachineIds.Contains(report.machine_id));
            }

            var latestReport = await latestReportQuery
            .AsNoTracking()
            .Where(report => report.report_date != null)
            .OrderByDescending(report => report.report_date)
            .Select(report => new
            {
                report.machine_id,
                report.lot_name,
                report.report_date
            })
            .FirstOrDefaultAsync(cancellationToken);

        SetDefaultDateRange(filter, latestReport?.report_date);

        if (!lineOptions.Any(option => option.Value == filter.LineName))
        {
            filter.LineName = null;
            selectedMachineIds = GetLastMachineIdsByLine(machines, filter.LineName);
        }

        var modelOptionQuery = dbContext.production_reports
            .AsNoTracking()
            .Include(report => report.machine)
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
            var startTime = filter.StartTime ?? StartOfDay;
            var startDateTime = filter.StartDate.Value.ToDateTime(startTime);
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

        var rowsQuery = query
            .GroupBy(report => new
            {
                LineName = report.machine != null && report.machine.line != null
                    ? report.machine.line
                    : string.Empty,
                Lane = report.machine != null && report.machine.lane.HasValue
                    ? report.machine.lane.Value.ToString()
                    : string.Empty,
                ModelName = report.lot_name ?? string.Empty,
                GroupName = report.mjs_id ?? string.Empty
            })
            .Select(group => new ProductionReportRow
            {
                LineName = group.Key.LineName,
                Lane = group.Key.Lane,
                ModelName = group.Key.ModelName,
                GroupName = group.Key.GroupName,
                ProducedQuantityPanel = group.Sum(report => (long)(report.count_board ?? 0)),
                ProducedQuantityPattern = group.Sum(report => (long)(report.count_module ?? 0)),
                StartTime = group.Min(report => report.report_date),
                EndTime = group.Max(report => report.report_date)
            })
            .OrderByDescending(row => row.EndTime)
            .ThenBy(row => row.LineName)
            .ThenBy(row => row.Lane)
            .ThenBy(row => row.ModelName)
            .ThenBy(row => row.GroupName);

        var totalRecords = await rowsQuery.CountAsync(cancellationToken);
        var pagination = ReportPaging.Create(filter.Page, totalRecords);
        filter.Page = pagination.Page;

        var rows = await rowsQuery
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new ProductionReportViewModel
        {
            Filter = filter,
            LineOptions = lineOptions,
            ModelOptions = modelOptions,
            Pagination = pagination,
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

    private static ProductionReportViewModel CreateDatabaseErrorViewModel(ProductionReportFilter filter, Exception exception)
    {
        return new ProductionReportViewModel
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

    private static string? ResolveSelection(
        string? requestedValue,
        IReadOnlyList<ReportSelectOption> options,
        string? preferredValue)
    {
        if (options.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(requestedValue)
            && options.Any(option => option.Value == requestedValue))
        {
            return requestedValue;
        }

        if (!string.IsNullOrWhiteSpace(preferredValue)
            && options.Any(option => option.Value == preferredValue))
        {
            return preferredValue;
        }

        return options[0].Value;
    }

    private static void NormalizeFilter(ProductionReportFilter filter)
    {
        filter.LineName = string.IsNullOrWhiteSpace(filter.LineName)
            ? null
            : filter.LineName.Trim();

        filter.ModelName = string.IsNullOrWhiteSpace(filter.ModelName)
            ? null
            : filter.ModelName.Trim();
    }

    private static void SetDefaultDateRange(ProductionReportFilter filter, DateTime? latestReportDate)
    {
        if (filter.StartDate.HasValue || filter.EndDate.HasValue)
        {
            return;
        }

        if (!latestReportDate.HasValue)
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
