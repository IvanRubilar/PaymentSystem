using Microsoft.EntityFrameworkCore;
using PaymentSystem.Core.Db;
using PaymentSystem.Core.Services;
using PaymentSystem.Worker; 

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(
                context.Configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("PaymentSystem.API")
            ));

        services.AddScoped<ICsvProcessorService, CsvProcessorService>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
