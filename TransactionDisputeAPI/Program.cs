using Scalar.AspNetCore;
using Serilog;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Infrastructure.Repositories;
using TransactionDisputePortal.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

    builder.Services.AddScoped<ISqlExecuter>(sp =>
    new SqlExecuter(builder.Configuration.GetConnectionString("DefaultConnection")!));
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    builder.Services.AddScoped<IAuditRepository, AuditRepository>();

    builder.Services.AddControllers();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorClient", policy =>
        {
            policy.AllowAnyOrigin()  
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.MapOpenApi();
    //I prefer ScalarApiReference over SwaggerUI, just looks better :D
    app.MapScalarApiReference();
    app.UseRouting();
    app.UseCors("AllowBlazorClient");
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush();
}
