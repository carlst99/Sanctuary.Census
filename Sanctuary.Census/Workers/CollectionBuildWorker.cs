﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanctuary.Census.Abstractions.CollectionBuilders;
using Sanctuary.Census.Abstractions.Database;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.CollectionBuilders;
using Sanctuary.Census.Common.Objects;
using Sanctuary.Census.Common.Services;
using Sanctuary.Census.ServerData.Internal.Abstractions.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sanctuary.Census.Workers;

/// <summary>
/// A service worker responsible for building the data collections.
/// </summary>
public class CollectionBuildWorker : BackgroundService
{
    private readonly ILogger<CollectionBuildWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CommonOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionBuildWorker"/> class.
    /// </summary>
    /// <param name="logger">The logging interface to use.</param>
    /// <param name="serviceScopeFactory">The service provider.</param>
    /// <param name="options">The configured common options.</param>
    public CollectionBuildWorker
    (
        ILogger<CollectionBuildWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CommonOptions> options
    )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Ensure the DB structure is setup
        foreach (PS2Environment env in Enum.GetValues<PS2Environment>())
        {
            await using AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
            IServiceProvider services = serviceScope.ServiceProvider;

            services.GetRequiredService<EnvironmentContextProvider>().Environment = env;
            await services.GetRequiredService<IMongoContext>().ScaffoldAsync(ct).ConfigureAwait(false);
        }

        // TODO: Quick-update collections? E.g. the world collection could be updated every 5m to better represent lock state.
        int dataCacheFailureCount = 0;
        while (!ct.IsCancellationRequested)
        {
            await using AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
            IServiceProvider services = serviceScope.ServiceProvider;
            services.GetRequiredService<EnvironmentContextProvider>().Environment = _options.DataSourceEnvironment;

            IClientDataCacheService clientDataCache = services.GetRequiredService<IClientDataCacheService>();
            IServerDataCacheService serverDataCache = services.GetRequiredService<IServerDataCacheService>();
            ILocaleDataCacheService localeDataCache = services.GetRequiredService<ILocaleDataCacheService>();
            IMongoContext mongoContext = services.GetRequiredService<IMongoContext>();

            try
            {
                _logger.LogDebug("Populating client data cache...");
                await clientDataCache.RepopulateAsync(ct).ConfigureAwait(false);
                _logger.LogDebug("Populating server data cache...");
                await serverDataCache.RepopulateAsync(ct).ConfigureAwait(false);
                _logger.LogDebug("Populating locale data cache...");
                await localeDataCache.RepopulateAsync(ct).ConfigureAwait(false);
                dataCacheFailureCount = 0;
                _logger.LogInformation("Caches updated successfully!");
            }
            catch (Exception ex)
            {
                if (++dataCacheFailureCount >= 5)
                {
                    _logger.LogCritical(ex, "Failed to cache data five times in a row! Collection builder is stopping");
                    return;
                }

                _logger.LogError(ex, "Failed to cache data. Will retry in 15s...");
                await Task.Delay(TimeSpan.FromSeconds(15), ct).ConfigureAwait(false);
                continue;
            }

            ICollectionBuilder[] collectionBuilders =
            {
                new CurrencyCollectionBuilder(),
                new ExperienceCollectionBuilder(),
                new FactionCollectionBuilder(),
                new FireGroupCollectionBuilder(),
                new FireGroupToFireModeCollectionBuilder(),
                new FireModeCollectionBuilder(),
                new FireModeToProjectileCollectionBuilder(),
                new ItemCollectionBuilder(),
                new ItemCategoryCollectionBuilder(),
                new ItemToWeaponCollectionBuilder(),
                new PlayerStateGroup2CollectionBuilder(),
                new ProfileCollectionBuilder(),
                new ProjectileCollectionBuilder(),
                new WeaponCollectionBuilder(),
                new WeaponAmmoSlotCollectionBuilder(),
                new WeaponToFireGroupCollectionBuilder(),
                new WorldCollectionBuilder()
            };

            foreach (ICollectionBuilder collectionBuilder in collectionBuilders)
            {
                try
                {
                    await collectionBuilder.BuildAsync
                    (
                        clientDataCache,
                        serverDataCache,
                        localeDataCache,
                        mongoContext,
                        ct
                    );
                    _logger.LogDebug("Successfully ran the {CollectionBuilder}", collectionBuilder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run the {CollectionBuilder}", collectionBuilder);
                }
            }
            _logger.LogInformation("Collections build complete");

            clientDataCache.Clear();
            serverDataCache.Clear();
            localeDataCache.Clear();
            _logger.LogDebug("Data caches cleared");

            await Task.Delay(TimeSpan.FromHours(1), ct).ConfigureAwait(false);
        }
    }
}
