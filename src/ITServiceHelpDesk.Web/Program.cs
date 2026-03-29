using ITServiceHelpDesk.Data;
using ITServiceHelpDesk.Models.Entities;
using ITServiceHelpDesk.Infrastructure.Extensions;
using ITServiceHelpDesk.Infrastructure.Identity;
using ITServiceHelpDesk.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERILOG CONFIGURATION
// ============================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// DATABASE CONFIGURATION
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

// ============================================
// IDENTITY CONFIGURATION
// ============================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================
// COOKIE CONFIGURATION
// ============================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// ============================================
// MICROSOFT SSO - OIDC CONFIGURATION
// ============================================
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
builder.Services.AddAuthentication()
    .AddOpenIdConnect("Microsoft", "Zaloguj przez Microsoft", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Authority = $"{azureAdConfig["Instance"]}{azureAdConfig["TenantId"]}/v2.0";
        options.ClientId = azureAdConfig["ClientId"]!;
        options.ClientSecret = azureAdConfig["ClientSecret"];
        options.ResponseType = "code";
        options.CallbackPath = azureAdConfig["CallbackPath"] ?? "/signin-microsoft";
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;

        // Zachowaj oryginalne nazwy claims z JWT (email, given_name, family_name)
        options.MapInboundClaims = false;

        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

// ============================================
// APPLICATION SERVICES
// ============================================
builder.Services.AddApplicationServices();

// ============================================
// MVC CONFIGURATION
// ============================================
builder.Services.AddControllersWithViews(options =>
{
    // Global filters can be added here
})
.AddRazorRuntimeCompilation();

// ============================================
// SESSION CONFIGURATION (for TempData)
// ============================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ============================================
// AUTHORIZATION POLICIES
// ============================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireAgentRole", policy => 
        policy.RequireRole("Admin", "Agent"));
    
    options.AddPolicy("RequireUserRole", policy => 
        policy.RequireRole("Admin", "Agent", "User"));
});

var app = builder.Build();

// ============================================
// DATABASE INITIALIZATION & SEEDING
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Apply pending migrations
        await context.Database.MigrateAsync();
        
        // Seed data
        await IdentitySeeder.SeedRolesAsync(roleManager);
        await IdentitySeeder.SeedUsersAsync(userManager);
        await IdentitySeeder.SeedCategoriesAsync(context);
        await IdentitySeeder.SeedSampleTicketsAsync(context, userManager);
        
        Log.Information("Database initialized and seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while initializing the database");
        throw;
    }
}

// ============================================
// MIDDLEWARE PIPELINE
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// ============================================
// ROUTE CONFIGURATION
// ============================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ============================================
// APPLICATION START
// ============================================
Log.Information("IT Service HelpDesk Application starting...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
