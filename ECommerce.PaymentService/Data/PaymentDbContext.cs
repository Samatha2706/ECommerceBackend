using ECommerce.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(
        DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.Amount)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.PaymentMethod)
                .HasMaxLength(50);
            entity.HasIndex(payment => payment.OrderId)
    .IsUnique();

        });
    }

    public DbSet<Payment> Payments { get; set; }
}