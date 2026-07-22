using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Services.Reports;

var builder = WebApplication.CreateBuilder(args);

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

var connStr = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
        npgsqlOptions.CommandTimeout(30);
    }));
builder.Services.AddScoped<IProductionReportService, ProductionReportService>();
builder.Services.AddScoped<IPickPlacementByPartService, PickPlacementByPartService>();
builder.Services.AddScoped<IPickPlacementByFeederService, PickPlacementByFeederService>();
builder.Services.AddScoped<IPickPlacementByNozzleService, PickPlacementByNozzleService>();
builder.Services.AddScoped<ICycleTimeReportService, CycleTimeReportService>();
builder.Services.AddScoped<IBoardCountChartService, BoardCountChartService>();
builder.Services.AddScoped<IDowntimeReportService, DowntimeReportService>();
builder.Services.AddScoped<ITotalPickupPlacementReportService, TotalPickupPlacementReportService>();
builder.Services.AddScoped<IOverallDashboardService, OverallDashboardService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
