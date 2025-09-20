using ClickMart.Interfaces;
using ClickMart.Repositorios;
using ClickMart.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================= EF Core =======================
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var cs = builder.Configuration.GetConnectionString("Conn");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
    options.EnableDetailedErrors();
    // options.EnableSensitiveDataLogging();
});

// ======================= DI =======================
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
builder.Services.AddScoped<IFacturaService, FacturaService>();

// ======================= Uploads (multipart) =======================
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
});

// ======================= JWT =======================
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
            RoleClaimType = "rol",
            NameClaimType = "uid"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ClienteOrAdmin", p => p.RequireRole("Cliente", "Admin"));
});

// ======================= Controllers + Swagger =======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClickMart API", Version = "v1" });

    // JWT en Swagger
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

    // 🔧 1) Esquemas únicos por nombre completo (evita CreateDTO duplicados)
    c.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace('+', '_'));

    // 🔧 2) OperationId único por controller/acción/verbo (evita colisiones)
    c.CustomOperationIds(api =>
    {
        var ctrl = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var cName) ? cName : "Controller";
        var act = api.ActionDescriptor.RouteValues.TryGetValue("action", out var aName) ? aName : "Action";
        var verb = api.HttpMethod ?? "HTTP";
        return $"{ctrl}_{act}_{verb}";
    });

    // 🔧 3) Si hubiese dos acciones con misma ruta/verbo, prioriza la primera
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

// ======================= QuestPDF =======================
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// ======================= Pipeline =======================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();   // ver stack en el navegador si algo rompe
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
