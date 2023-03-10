using FeedToTeams.Frontend.Models;
using FeedToTeams.Frontend.Utilities;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FeedToTeamsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("FeedToTeamsDbConnection"), sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
          maxRetryCount: 5,
          maxRetryDelay: TimeSpan.FromSeconds(3),
          errorNumbersToAdd: null);
    }));
builder.Services.AddScoped<IMsalAccountActivityStore, SqlServerMsalAccountActivityStore>();
var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ') ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ');

builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("FeedToTeamsDbConnection");
    options.SchemaName = "dbo";
    options.TableName = "sqlTokenCache";
    options.DefaultSlidingExpiration = TimeSpan.FromDays(1);
});

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddIntegratedUserTokenCache();

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        ValidateIssuer = false,
        SaveSigninToken = true
    };
    options.Events.OnSignedOutCallbackRedirect = (context) =>
    {
        context.Response.Redirect("/");
        context.HandleResponse();
        return Task.CompletedTask;
    };
});

var app = builder.Build();

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
app.MapControllers();

app.Run();
