using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ===== MVC + Helpers =====
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ===== HttpClient "Api" (usa ApiBaseUrl de appsettings.json) =====
// Ejemplo appsettings.json (MVC):
// { "ApiBaseUrl": "https://localhost:7069/api/" }
builder.Services.AddHttpClient("Api", c =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Config 'ApiBaseUrl' no está definida.");

    // Normaliza para terminar en /api/
    if (!baseUrl.EndsWith("/")) baseUrl += "/";
    if (!baseUrl.EndsWith("api/")) baseUrl = baseUrl.TrimEnd('/') + "/api/";

    c.BaseAddress = new Uri(baseUrl);
    c.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

// ===== Auth por Cookie (el MVC protege vistas con cookie) =====
builder.Services.AddAuthentication("AuthCookie")
    .AddCookie("AuthCookie", opts =>
    {
        opts.LoginPath = "/Auth/Login";
        opts.LogoutPath = "/Auth/Logout";
        opts.AccessDeniedPath = "/Auth/Denied"; // asegúrate que exista
        opts.SlidingExpiration = true;
        opts.Cookie.Name = ".ClickMart.Auth";
        opts.Cookie.HttpOnly = true;
        // opts.Cookie.SameSite = SameSiteMode.Lax;
        // opts.Cookie.SecurePolicy = CookieSecurePolicy.Always; // si todo es HTTPS
    });

// ===== Autorización (políticas opcionales) =====
builder.Services.AddAuthorization(options =>
{
    // Acepta “Admin”, “Administrador” y el typo “adminitrador”
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("Admin", "Administrador", "adminitrador"));
});

// ===== Servicios (DI) =====
builder.Services.AddScoped<ClickMart.web.Services.ApiService>();
builder.Services.AddScoped<ClickMart.web.Services.AuthService>();
builder.Services.AddScoped<ClickMart.web.Services.ProductoService>();
builder.Services.AddScoped<ClickMart.web.Services.DistribuidorService>(); // ?? FALTABA
builder.Services.AddScoped<ClickMart.web.Services.CategoriaService>();
builder.Services.AddScoped<ClickMart.web.Services.CatalogoService>();
builder.Services.AddScoped<ClickMart.web.Services.UsuarioService>();
builder.Services.AddScoped<ClickMart.web.Services.RolService>();


var app = builder.Build();

// ===== Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // cookie
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
