using ECommerce.UserService.Data;
using ECommerce.UserService.DTOs.Auth;
using ECommerce.UserService.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UserService.Services;

public class AuthService : IAuthService
{
    private readonly UserDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        UserDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto registerDto)
    {
        var email = registerDto.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(user => user.Email == email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var user = new User
        {
            FullName = registerDto.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(
                registerDto.Password),
            Role = UserRole.Customer
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.Role.ToString());

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = token
        };
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto loginDto)
    {
        var email = loginDto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                existingUser => existingUser.Email == email);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var passwordValid = _passwordHasher.VerifyPassword(
            loginDto.Password,
            user.PasswordHash);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.Role.ToString());

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = token
        };
    }
}