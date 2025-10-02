using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ClickMart.web.Helpers; // para ClaimsHelper.IsJwtExpired

var builder = WebApplication.CreateBuilder(args);

// ===== MVC + Helpers =====
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ===== HttpClient "Api" (usa ApiBaseUrl de appsettings.json) =====
// Ejemplo appsettings.json (MVC):
//{ "ApiBaseUrl": "https://localhost:7069/api/" }
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
// Nota: AddHttpClient registra IHttpClientFactory automáticamente.
// Nuestro PedidoService llamará CreateClient("Api") para traer el PDF.

// ===== Auth por Cookie (el MVC protege vistas con cookie) =====
builder.Services.AddAuthentication("AuthCookie")
    .AddCookie("AuthCookie", opts =>
    {
        opts.LoginPath = "/Auth/Login";
        opts.LogoutPath = "/Auth/Logout";
        opts.AccessDeniedPath = "/Auth/Denied";
        opts.Cookie.Name = ".ClickMart.Auth";
        opts.Cookie.HttpOnly = true;

        // Recomendado en flujos web:
        // opts.Cookie.SameSite = SameSiteMode.Lax;
        // Si estás 100% en HTTPS:
        // opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        opts.SlidingExpiration = true;
        opts.ExpireTimeSpan = TimeSpan.FromHours(8);

        // Si el JWT (claim "token") expira, invalida el cookie automáticamente
        opts.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                try
                {
                    var token = context.Principal?.FindFirst("token")?.Value;
                    if (ClaimsHelper.IsJwtExpired(token))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync("AuthCookie");
                    }
                }
                catch
                {
                    // no-op: si algo falla, dejamos que el request siga
                }
            }
        };
    });

// ===== Autorización (políticas opcionales) =====
builder.Services.AddAuthorization(options =>
{
    // Acepta alias clásicos, sin typos
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("Admin", "Administrador", "Administrator"));
});

// ===== Servicios (DI) =====
builder.Services.AddScoped<ClickMart.web.Services.ApiService>();
builder.Services.AddScoped<ClickMart.web.Services.AuthService>();
builder.Services.AddScoped<ClickMart.web.Services.ProductoService>();
builder.Services.AddScoped<ClickMart.web.Services.DistribuidorService>();
builder.Services.AddScoped<ClickMart.web.Services.CategoriaService>();
builder.Services.AddScoped<ClickMart.web.Services.CatalogoService>();
builder.Services.AddScoped<ClickMart.web.Services.UsuarioService>();
builder.Services.AddScoped<ClickMart.web.Services.RolService>();
builder.Services.AddScoped<ClickMart.web.Services.ResenaService>();
builder.Services.AddScoped<ClickMart.web.Services.PedidoService>();           // ? PedidoService ahora recibe ApiService + IHttpClientFactory
builder.Services.AddScoped<ClickMart.web.Services.DetallePedidoService>();
builder.Services.AddScoped<ClickMart.web.Services.UsuarioApiService>();
builder.Services.AddScoped<ClickMart.web.Services.ProductoCatalogService>();

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
