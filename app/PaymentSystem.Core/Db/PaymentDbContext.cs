using Microsoft.EntityFrameworkCore;
using PaymentSystem.Core.Models;

namespace PaymentSystem.Core.Db;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Transferencia> Transferencias { get; set; }
}
