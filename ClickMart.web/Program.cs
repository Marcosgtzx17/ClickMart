using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// HttpClient nombrado "Api" que lee ApiBaseUrl
builder.Services.AddHttpClient("Api", c =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Config 'ApiBaseUrl' no está definida.");

    // Normaliza para evitar sorpresas
    if (!baseUrl.EndsWith("/")) baseUrl += "/";
    if (!baseUrl.EndsWith("/api/")) baseUrl = baseUrl.TrimEnd('/') + "/api/";

    c.BaseAddress = new Uri(baseUrl);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Auth por cookies (si ya lo tienes, déjalo igual)
builder.Services.AddAuthentication("AuthCookie")
    .AddCookie("AuthCookie", opts =>
    {
        opts.LoginPath = "/Auth/Login";
        opts.LogoutPath = "/Auth/Logout";
        opts.AccessDeniedPath = "/Auth/Login";
        opts.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Tus servicios
builder.Services.AddScoped<ClickMart.web.Services.ApiService>();
builder.Services.AddScoped<ClickMart.web.Services.AuthService>();
// builder.Services.AddScoped<CategoriaApiService>(); // si lo usas

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

app.Run();