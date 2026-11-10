namespace ECommerce.UserService.Services;

public interface IJwtTokenService
{
    string GenerateToken(
        int userId,
        string email,
        string role);
}