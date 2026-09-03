using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Moq;

namespace ECommerce.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IGenericRepository<User>>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldRegisterNewCustomer()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "John@Example.com",
            Password = "Password@123"
        };

        _userRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<User>());

        _passwordHasherMock
            .Setup(hasher => hasher.HashPassword("Password@123"))
            .Returns("hashed-password");

        _jwtTokenServiceMock
            .Setup(jwt => jwt.GenerateToken(
                It.IsAny<int>(),
                "john@example.com",
                "Customer"))
            .Returns("test-jwt-token");

        _userRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.Equal("test-jwt-token", result.Token);

        _passwordHasherMock.Verify(
            hasher => hasher.HashPassword("Password@123"),
            Times.Once);

        _userRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<User>(user =>
                    user.Email == "john@example.com" &&
                    user.FullName == "John Doe" &&
                    user.Role == UserRole.Customer)),
            Times.Once);

        _userRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "Password@123"
        };

        var existingUser = new User
        {
            Id = 1,
            FullName = "Existing User",
            Email = "john@example.com",
            PasswordHash = "existing-hash",
            Role = UserRole.Customer
        };

        _userRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<User> { existingUser });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(registerDto));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        _userRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john@example.com",
            Password = "Password@123"
        };

        var user = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Customer
        };

        _userRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<User> { user });

        _passwordHasherMock
            .Setup(hasher => hasher.VerifyPassword(
                "Password@123",
                "hashed-password"))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(jwt => jwt.GenerateToken(
                1,
                "john@example.com",
                "Customer"))
            .Returns("test-jwt-token");

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.Equal(1, result.UserId);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.Equal("test-jwt-token", result.Token);

        _passwordHasherMock.Verify(
            hasher => hasher.VerifyPassword(
                "Password@123",
                "hashed-password"),
            Times.Once);

        _jwtTokenServiceMock.Verify(
            jwt => jwt.GenerateToken(
                1,
                "john@example.com",
                "Customer"),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsInvalid()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Customer
        };

        _userRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<User> { user });

        _passwordHasherMock
            .Setup(hasher => hasher.VerifyPassword(
                "WrongPassword",
                "hashed-password"))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginDto));

        Assert.Equal(
            "Invalid email or password.",
            exception.Message);

        _jwtTokenServiceMock.Verify(
            jwt => jwt.GenerateToken(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }
}