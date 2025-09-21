using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// MVC + helpers
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// HttpClient nombrado "Api" que lee ApiBaseUrl
builder.Services.AddHttpClient("Api", c =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Config 'ApiBaseUrl' no est� definida.");

    // Normaliza para terminar en /api/
    if (!baseUrl.EndsWith("/")) baseUrl += "/";
    if (!baseUrl.EndsWith("/api/")) baseUrl = baseUrl.TrimEnd('/') + "/api/";

    c.BaseAddress = new Uri(baseUrl);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Autenticaci�n por cookies
builder.Services.AddAuthentication("AuthCookie")
    .AddCookie("AuthCookie", opts =>
    {
        opts.LoginPath = "/Auth/Login";
        opts.LogoutPath = "/Auth/Logout";
        opts.AccessDeniedPath = "/Auth/Denied"; // <- importante: 403 ya no redirige a Login
        opts.SlidingExpiration = true;
        opts.Cookie.Name = ".ClickMart.Auth";
        opts.Cookie.HttpOnly = true;
        // opts.Cookie.SameSite = SameSiteMode.Lax; // (opcional)
        // opts.Cookie.SecurePolicy = CookieSecurePolicy.Always; // si todo es HTTPS
    });

// Autorizaci�n (define pol�ticas UNA sola vez)
builder.Services.AddAuthorization(options =>
{
    // Pol�tica que acepta Admin, Administrador y el typo legacy adminitrador
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("Admin", "Administrador", "adminitrador"));
});

// Servicios de la app (DI)
builder.Services.AddScoped<ClickMart.web.Services.ApiService>();
builder.Services.AddScoped<ClickMart.web.Services.AuthService>();
builder.Services.AddScoped<ClickMart.web.Services.ProductoService>();


var app = builder.Build();

// Pipeline
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

app.Run();
