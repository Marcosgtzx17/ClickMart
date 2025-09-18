using ClickMart.Interfaces;
using ClickMart.Repositorios;   // AppDbContext, repos
using ClickMart.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;     // FormOptions (multipart)
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;               // QuestPDF license
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =======================
// EF Core + MySQL (Pomelo)
// =======================
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var cs = builder.Configuration.GetConnectionString("Conn");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
    options.EnableDetailedErrors();
    // options.EnableSensitiveDataLogging(); // solo en dev si lo necesitas
});

// =======================
// DI (Repos/Servicios)
// =======================
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAuthService, AuthRepository>();

builder.Services.AddScoped<ICategoriaProductoRepository, CategoriaProductoRepository>();
builder.Services.AddScoped<ICategoriaProductoService, CategoriaProductoService>();

builder.Services.AddScoped<IDistribuidorRepository, DistribuidorRepository>();
builder.Services.AddScoped<IDistribuidorService, DistribuidorService>();

builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();

builder.Services.AddScoped<IResenaRepository, ResenaRepository>();
builder.Services.AddScoped<IResenaService, ResenaService>();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

builder.Services.AddScoped<IDetallePedidoRepository, DetallePedidoRepository>();
builder.Services.AddScoped<IDetallePedidoService, DetallePedidoService>();

builder.Services.AddScoped<ICodigoConfirmacionRepository, CodigoConfirmacionRepository>();
builder.Services.AddScoped<ICodigoConfirmacionService, CodigoConfirmacionService>();

// Factura (PDF)
builder.Services.AddScoped<IFacturaService, FacturaService>();

// =======================
// Uploads (multipart)
// =======================
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
});

// =======================
// JWT Bearer
// =======================
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1),

            // 👇 NUEVO: para que [Authorize(Roles="Admin")] funcione con tu claim "rol"
            RoleClaimType = "rol",
            // 👇 NUEVO (opcional): que User.Identity.Name sea tu "uid"
            NameClaimType = "uid"
        };
    });

// =======================
// Autorización (opcional, pero cómodo)
// =======================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ClienteOrAdmin", p => p.RequireRole("Cliente", "Admin"));
});

// =======================
// Controllers + Swagger
// =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClickMart API", Version = "v1" });

    // Soporte JWT en Swagger (Authorize)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT en header. Ej: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// =======================
// QuestPDF (licencia community)
// =======================
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// =======================
// Middleware pipeline
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();   // <- primero
app.UseAuthorization();

app.MapControllers();

app.Run();
