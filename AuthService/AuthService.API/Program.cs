using AuthService.API.Extensions;
using AuthService.Application;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Persistence;
using Common.API.Health;
using Common.API.Middlewares;
using Common.API.Security;
using Common.Application.Abstractions;
using Common.Infrastructure.Observability;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddCommonSerilog("auth-service");
builder.AddCommonOpenTelemetry("auth-service");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddLocalJwtAuthentication();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks().AddDbContextCheck<AuthDbContext>();

WebApplication app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCommonHealthChecks();

await app.RunAsync();

public partial class Program;
