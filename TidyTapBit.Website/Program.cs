using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using TidyTrader.ApiIntegration.Interfaces;
using TidyTrader.ApiIntegration.Models;
using TidyTrader.Website.Data;
using TidyTrader.Website.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Register WebSocket clients
string apiKey = "dd9ac82aaedec750922f3e6fc5438816";
string apiSecret = "4ac673c254b5affa65549a2ed5f25c76";


// ApiIntegration
//builder.Services.AddSingleton<IBitunixApiClient>(sp =>  new BitunixApiClient(apiKey, apiSecret));
//builder.Services.AddSingleton<IMarketData>(sp => new MarketData(sp.GetRequiredService<IBitunixApiClient>(), /* leverageConfig */);

// Core
//builder.Services.AddScoped<MarketService>();

// Register the hosted service
builder.Services.AddHostedService<WebSocketHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();


app.Run();
