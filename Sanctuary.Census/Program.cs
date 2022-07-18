using FluentMigrator.Runner;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sanctuary.Census.ClientData.Extensions;
using Sanctuary.Census.Common.Objects;
using Sanctuary.Census.Database.Migrations;
using Sanctuary.Census.Json;
using Sanctuary.Census.Middleware;
using Sanctuary.Census.ServerData.Internal.Extensions;
using Sanctuary.Census.ServerData.Internal.Objects;
using Sanctuary.Census.Workers;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Sanctuary.Census;

/// <summary>
/// The main class of the application.
/// </summary>
public static class Program
{
    private const string LOGGING_OPTIONS_NAME = "LoggingOptions";

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">Runtime arguments to be passed to the application.</param>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        if (!builder.Environment.IsDevelopment() && OperatingSystem.IsLinux())
            builder.Host.UseSystemd();

        string? seqIngestionEndpoint = builder.Configuration.GetSection(LOGGING_OPTIONS_NAME).GetSection("SeqIngestionEndpoint").Value;
        string? seqApiKey = builder.Configuration.GetSection(LOGGING_OPTIONS_NAME).GetSection("SeqApiKey").Value;
        SetupLogger(seqIngestionEndpoint, seqApiKey);
        builder.Host.UseSerilog();

        builder.Services.Configure<CommonOptions>(builder.Configuration.GetSection(nameof(CommonOptions)))
            .Configure<LoginClientOptions>(builder.Configuration.GetSection(nameof(LoginClientOptions)))
            .Configure<GatewayClientOptions>(builder.Configuration.GetSection(nameof(GatewayClientOptions)));

        builder.Services.AddClientDataServices()
            .AddInternalServerDataServices();

        builder.Services.AddSingleton<CollectionsContext>();
        builder.Services.AddHostedService<CollectionBuildWorker>();

        builder.Services.AddFluentMigratorCore()
            .ConfigureRunner
            (
                rb => rb.AddMySql5()
                    .WithGlobalConnectionString(builder.Configuration.GetConnectionString("SqlConnection"))
                    .ScanIn(typeof(InitialTables20220717001).Assembly).For.Migrations()
            );

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseJsonNamingPolicy();
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new DataResponseJsonConverter());
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<EnumSchemaFilter>();

            IEnumerable<Assembly> assems = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => a.FullName?.StartsWith("Sanctuary.Census") == true);

            foreach (Assembly a in assems)
            {
                string xmlFilename = $"{a.GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            }
        });

        WebApplication app = builder.Build();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseSerilogRequestLogging();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
            app.UseExceptionHandler("/error");
        else
            app.UseDeveloperExceptionPage();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = "api-doc";
        });

        app.UseHttpsRedirection();
        app.UseMiddleware<ServiceIDMiddleware>();

        app.MapGet("/", delegate(HttpContext context)
        {
            context.Response.Redirect("https://github.com/carlst99/Sanctuary.Census");
        });

        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();

        // Migrate database
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IMigrationRunner runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }

        app.Run();
    }

    // ReSharper disable twice UnusedParameter.Local
    private static void SetupLogger(string? seqIngestionEndpoint, string? seqApiKey)
    {
        LoggerConfiguration loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

#if !DEBUG
        if (!string.IsNullOrEmpty(seqIngestionEndpoint) && !string.IsNullOrEmpty(seqApiKey))
        {
            Serilog.Core.LoggingLevelSwitch levelSwitch = new();
            loggerConfig.MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Seq(seqIngestionEndpoint, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
        }
#endif

        Log.Logger = loggerConfig.CreateLogger();
    }
}
