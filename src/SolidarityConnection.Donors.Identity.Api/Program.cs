using Azure.Messaging.ServiceBus;
using SolidarityConnection.Donors.Identity.Api.Extensions;
using SolidarityConnection.Donors.Identity.Api.Middlewares;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Publishers;
using SolidarityConnection.Donors.Identity.Application.Interfaces.Services;
using SolidarityConnection.Donors.Identity.Application.Services;
using SolidarityConnection.Donors.Identity.Domain.Interfaces.Repositories;
using SolidarityConnection.Donors.Identity.Infrastructure.Data;
using SolidarityConnection.Donors.Identity.Infrastructure.Publishers;
using SolidarityConnection.Donors.Identity.Infrastructure.Repositories;
using SolidarityConnection.Donors.Identity.Infrastructure.ServiceBus;
using SolidarityConnection.Donors.Identity.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddFiapCloudGamesSwagger();

builder.Services.AddFiapCloudGamesOpenTelemetry();

builder.Services.AddFiapCloudGamesJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection") ?? ""));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var sbConnectionString = configuration["ServiceBus:ConnectionString"] ?? "";
builder.Services.Configure<ServiceBusOptions>(opts => { opts.ConnectionString = sbConnectionString; });

builder.Services.AddSingleton(new ServiceBusClient(sbConnectionString));
builder.Services.AddSingleton<IServiceBusClientWrapper, ServiceBusClientWrapper>();
builder.Services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();

builder.Services.AddScoped<IUserEventPublisher, UserEventPublisher>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TracingEnrichmentMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

