using Microsoft.EntityFrameworkCore;
using PaymentSystem.Core.Db;
using PaymentSystem.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar el DbContext con PostgreSQL
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("PaymentSystem.API") 
    ));


//servicios 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ICsvProcessorService, CsvProcessorService>();

var app = builder.Build();

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.UseAuthorization();

app.MapControllers(); 
app.Urls.Add("http://0.0.0.0:80");
app.Run();
