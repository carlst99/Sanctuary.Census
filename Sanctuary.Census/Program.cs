using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.ClientData.Services;
using System.Collections.Generic;
using System.Linq;

namespace Sanctuary.Census;

/// <summary>
/// The main class of the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Gets the directory under which any app data should be stored.
    /// </summary>
    public static readonly string AppDataDirectory = Path.Combine
    (
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sanctuary.Census"
    );

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">Runtime arguments to be passed to the application.</param>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
        builder.Services.AddHttpClient<IManifestService, DebugManifestService>(h => new DebugManifestService(h, AppDataDirectory));
#else
        builder.Services.AddHttpClient<IManifestService, CachingManifestService>(h => new CachingManifestService(h, AppDataDirectory));
#endif

        builder.Services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
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

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = "api-doc";
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}