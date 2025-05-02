using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using TidyTapBit.ApiIntegration.Models;
using TidyTapBit.Website.Data;
using TidyTapBit.Website.Services;

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


builder.Services.AddSingleton<BitunixWebSocketClient>(sp => new BitunixWebSocketClient(apiKey, apiSecret, "wss://fapi.bitunix.com/public"));
builder.Services.AddSingleton<BitunixWebSocketClient>(sp => new BitunixWebSocketClient(apiKey, apiSecret, "wss://fapi.bitunix.com/private"));


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
