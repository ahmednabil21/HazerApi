using HazarApi.Data;
using HazarApi.DTO.Auth;
using HazarApi.DTO.Common;
using HazarApi.Entities;
using HazarApi.IServices.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using AutoMapper;

namespace HazarApi.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(AppDbContext context, ILogger<AuthService> logger, IConfiguration configuration, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null, string? userAgent = null)
    {
        _logger.LogInformation("Authenticating user {Username}", request.Username);

        var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == request.Username && !e.IsDeleted);

        if (employee == null || !employee.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // تسجيل جلسة الدخول
        var session = new UserSession
        {
            EmployeeId = employee.Id,
            LoginTime = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true
        };
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(employee);
        var refreshToken = GenerateRefreshToken();

        return new AuthResponseDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Username = employee.Username,
            Role = employee.Role?.Type.ToString() ?? "Employee"
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        _logger.LogInformation("Refreshing token.");

        // For simplicity, we'll just validate and generate a new token
        // In production, you should store refresh tokens in the database
        var principal = GetPrincipalFromExpiredToken(request.RefreshToken);
        var username = principal?.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == username && !e.IsDeleted && e.IsActive);

        if (employee == null)
        {
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        var token = GenerateJwtToken(employee);
        var refreshToken = GenerateRefreshToken();

        return new AuthResponseDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Username = employee.Username,
            Role = employee.Role?.Type.ToString() ?? "Employee"
        };
    }

    private string GenerateJwtToken(Employee employee)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");
        var issuer = jwtSettings["Issuer"] ?? "HazarApi";
        var audience = jwtSettings["Audience"] ?? "HazarApiUsers";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, employee.Username),
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Role, employee.Role?.Type.ToString() ?? "Employee"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    private int GetJwtExpirationMinutes()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        return int.TryParse(jwtSettings["ExpirationMinutes"], out var minutes) ? minutes : 60;
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResponseDto> LogoutAsync(int employeeId)
    {
        _logger.LogInformation("Logging out employee {EmployeeId}", employeeId);

        // إغلاق جميع الجلسات النشطة للموظف
        var activeSessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId && s.IsActive && !s.IsDeleted)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.LogoutTime = DateTime.UtcNow;
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new ApiResponseDto
        {
            Success = true,
            Message = "Logged out successfully."
        };
    }

    public async Task<IReadOnlyCollection<UserSessionDto>> GetUserSessionsAsync(int employeeId, int? days = 30)
    {
        _logger.LogInformation("Fetching sessions for employee {EmployeeId} for last {Days} days", employeeId, days);

        var cutoffDate = DateTime.UtcNow.AddDays(-(days ?? 30));

        var sessions = await _context.UserSessions
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == employeeId &&
                       s.LoginTime >= cutoffDate &&
                       !s.IsDeleted)
            .OrderByDescending(s => s.LoginTime)
            .ToListAsync();

        return _mapper.Map<List<UserSessionDto>>(sessions);
    }
}
