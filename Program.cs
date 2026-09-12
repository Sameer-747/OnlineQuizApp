using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC + Razor Pages (Identity UI uses Razor Pages)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddScoped<OnlineQuizApp.Services.BadgeService>();

var app = builder.Build();

// Apply migrations and seed roles/admin user automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.

// Render (and most reverse-proxy hosts) terminate HTTPS at their edge and forward requests to
// this app over plain HTTP internally. Without this, the app thinks every request is HTTP,
// which breaks anything that builds an absolute URL from the request scheme (e.g. the login
// redirect on a failed auth challenge gets stamped "http://" and the browser blocks it as mixed
// content on an https page). Render's proxy IP isn't fixed, so clear the known-networks/proxies
// restriction to trust the forwarded headers unconditionally.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
