using ECommerce.UserService.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}