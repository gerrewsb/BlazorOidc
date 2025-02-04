using BlazorOidc;
using BlazorOidc.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = "MyAuthority";
    options.ClientId = "MyClientId";
    options.ClientSecret = "MyClientSecret";

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "groups",
        NameClaimType = "name",

        ValidAudience = "MyClientId"
    };
    options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
    options.Scope.Add(OpenIdConnectScope.Email);
    options.Scope.Add(OpenIdConnectScope.OfflineAccess);
    options.MapInboundClaims = false;
    options.CallbackPath = new PathString("/signin-oidc");
    options.SignedOutCallbackPath = new PathString("/signout-callback-oidc");
    options.RemoteSignOutPath = new PathString("/signout-oidc");
});

builder.Services.AddSingleton<OidcCookieRefresher>();
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<OidcCookieRefresher>((options, refresher) =>
    {
        options.Events.OnValidatePrincipal = context => refresher.ValidateOrRefreshCookieAsync(context);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

//ORDER IS IMPORTANT: routing => authentication => authorization => antiforgery => endpoints
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

//ENDPOINTS
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapBlazorHub(options =>
{
    options.CloseOnAuthenticationExpiration = true;
}).WithOrder(-1);

app.MapRazorPages();

app.MapGroup("/authentication")
    .MapLoginAndLogout();

app.Run();
