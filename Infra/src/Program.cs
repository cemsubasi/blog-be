﻿using System.Text.Json.Serialization;
using Domain.Account.Entity;
using Domain.SuperAccount.Entity;
using Infra;
using Infra.Configurations;
using Infra.Context;
using Infra.Filters;
using Infra.Middlewares;
using Infra.Post;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Serilog;
using Serilog.Events;

var configuration = new ConfigurationBuilder()
  .AddCommandLine(args)
  .Build();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var configurationBuilder = new ConfigurationBuilder()
  .AddCommandLine(args)
  .AddJsonFile($"config.json");

if (!string.IsNullOrEmpty(configuration["environment"])) {
  configurationBuilder.AddJsonFile($"config.{configuration["environment"]}.json");
}

configuration = configurationBuilder.Build();

Log.Information("Environment is {0}", configuration["environment"]);

var config = TypeAdapterConfig.GlobalSettings;
config.AllowImplicitDestinationInheritance = true;
PostMapper.Map(config);

builder.Services.AddControllers(options => {
  options.Filters.Add<HttpResponseExceptionFilter>();
  /* options.Filters.Add<CustomExceptionFilter>(); */
});

builder.Services.AddControllers()
  .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddAuthentication(args, configuration);
builder.Services.AddAuthorization(options => {
  options.AddPolicy(typeof(SuperAccountEntity).Name, policy => policy.RequireRole(typeof(SuperAccountEntity).Name));
  options.AddPolicy(typeof(AccountEntity).Name, policy => policy.RequireRole(typeof(AccountEntity).Name));
});
builder.Services.Configure<Infra.Configurations.SentryOptions>(configuration.GetSection("Sentry"));
builder.Services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
builder.Services.Configure<DBOptions>(configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddUtils(ServiceLifetime.Singleton);
builder.Services.AddServices(ServiceLifetime.Scoped);
builder.Services.AddAccountUseCaseHandlers(ServiceLifetime.Scoped);
builder.Services.AddSuperAccountUseCaseHandlers(ServiceLifetime.Scoped);
builder.Services.AddPostUseCaseHandlers(ServiceLifetime.Scoped);
builder.Services.AddSwagger();
builder.Services.AddDbContext<MainDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Host.UseSerilog();
builder.WebHost.AddSentry(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();

  app.Lifetime.ApplicationStarted.Register(() => {
    var server = app.Services.GetRequiredService<IServer>();
    var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
    if (addressesFeature != null) {
      foreach (var address in addressesFeature.Addresses) {
        Log.Write(LogEventLevel.Information, "Swagger: {0}{1}", address, "/swagger/index.html");
      }
    }
  });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionMiddleware>();

app.UseHttpsRedirection();
app.UseCors(x => x.WithOrigins("localhost:3000").AllowCredentials());

app.MapControllers();

app.Run();
