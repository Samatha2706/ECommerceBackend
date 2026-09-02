using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    public AuthService(
    IGenericRepository<User> userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto registerDto)
    {
        var email = registerDto.Email.Trim().ToLowerInvariant();

        var users = await _userRepository.GetAllAsync();

        var emailExists = users.Any(user =>
            user.Email.Equals(
                email,
                StringComparison.OrdinalIgnoreCase));

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

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

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

        var users = await _userRepository.GetAllAsync();

        var user = users.FirstOrDefault(existingUser =>
            existingUser.Email.Equals(
                email,
                StringComparison.OrdinalIgnoreCase));

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