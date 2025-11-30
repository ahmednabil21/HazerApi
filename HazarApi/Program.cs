using HazarApi.Data;
using HazarApi.IServices.Attendance;
using HazarApi.IServices.Auth;
using HazarApi.IServices.Dashboard;
using HazarApi.IServices.Employees;
using HazarApi.IServices.Policies;
using HazarApi.IServices.Summary;
using HazarApi.Mapper;
using HazarApi.Middleware;
using HazarApi.Services.Attendance;
using HazarApi.Services.Auth;
using HazarApi.Services.Dashboard;
using HazarApi.Services.Employees;
using HazarApi.Services.Policies;
using HazarApi.Services.Summary;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(Directory.GetCurrentDirectory(), "Logs", "hazarapi-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting HazarApi application");

    // Add assembly resolver to help find Microsoft.Data.SqlClient
    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
    {
        var assemblyName = new System.Reflection.AssemblyName(args.Name);
        if (assemblyName.Name == "Microsoft.Data.SqlClient")
        {
            var basePath = AppContext.BaseDirectory;
            var dllPath = Path.Combine(basePath, "Microsoft.Data.SqlClient.dll");
            if (File.Exists(dllPath))
            {
                return System.Reflection.Assembly.LoadFrom(dllPath);
            }
            // Try runtimes folder
            var runtimePath = Path.Combine(basePath, "runtimes", "win", "lib", "net6.0", "Microsoft.Data.SqlClient.dll");
            if (File.Exists(runtimePath))
            {
                return System.Reflection.Assembly.LoadFrom(runtimePath);
            }
        }
        return null;
    };

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hazar Attendance Management API",
        Version = "v1",
        Description = "نظام إدارة الدوام والموظفين"
    });

    // إضافة JWT Authentication إلى Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Ignore errors in schema generation
    options.CustomSchemaIds(type => type.FullName);
});

var connectionString = builder.Configuration.GetConnectionString("Default")
                        ?? throw new InvalidOperationException("Connection string 'Default' is missing.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");
var issuer = jwtSettings["Issuer"] ?? "HazarApi";
var audience = jwtSettings["Audience"] ?? "HazarApiUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// CORS Configuration
var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
                  ?? new[] { "http://localhost:7065", "https://localhost:7065" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IMonthlySummaryService, MonthlySummaryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAttendancePolicyService, AttendancePolicyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Exception Handling Middleware should be first
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable routing
app.UseRouting();

// CORS must be after UseRouting and before UseAuthentication
app.UseCors("AllowFrontend");

// Static files for Swagger UI assets
app.UseStaticFiles();

// Swagger must be before Authentication/Authorization
// Note: Swagger should not require authentication
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
    c.SerializeAsV2 = false;
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hazar Attendance Management API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableValidator();
});

// HTTPS Redirection
// ملاحظة: في IIS (Somee)، web.config يتولى HTTPS redirection
// يمكن تفعيله هنا أيضاً إذا لزم الأمر
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Root endpoint - API information
app.MapGet("/", () => Results.Json(new
{
    name = "Hazar Attendance Management API",
    version = "v1",
    description = "نظام إدارة الدوام والموظفين",
    documentation = "/swagger",
    swaggerJson = "/swagger/v1/swagger.json",
    status = "running",
    timestamp = DateTime.UtcNow
})).WithTags("Info");

// Test endpoint
app.MapGet("/api/test", () => Results.Json(new
{
    message = "API is working",
    timestamp = DateTime.UtcNow
})).WithTags("Test");

// Map controllers
app.MapControllers();

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