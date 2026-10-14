using GLMS.Web.Data;
using GLMS.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<ExchangeRateOptions>(builder.Configuration.GetSection("ExchangeRate"));
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>(c =>
    c.Timeout = TimeSpan.FromSeconds(5));

builder.Services.AddScoped<ICurrencyCalculator, CurrencyCalculator>();
builder.Services.AddScoped<IFileValidator, FileValidator>();
builder.Services.AddScoped<ContractWorkflowService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
