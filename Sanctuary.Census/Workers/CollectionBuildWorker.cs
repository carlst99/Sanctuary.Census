﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanctuary.Census.Abstractions.CollectionBuilders;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.CollectionBuilders;
using Sanctuary.Census.Common.Abstractions.Services;
using Sanctuary.Census.Common.Objects;
using Sanctuary.Census.Common.Services;
using Sanctuary.Census.Controllers;
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
    private readonly CollectionsContext _collectionsContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContributionController"/> class.
    /// </summary>
    /// <param name="logger">The logging interface to use.</param>
    /// <param name="serviceScopeFactory">The service provider.</param>
    /// <param name="options">The configured common options.</param>
    /// <param name="collectionsContext">The collections context.</param>
    public CollectionBuildWorker
    (
        ILogger<CollectionBuildWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CommonOptions> options,
        CollectionsContext collectionsContext
    )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _collectionsContext = collectionsContext;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // TODO: Quick-update collections? E.g. the world collection could be updated every 5m to better represent lock state.
        int dataCacheFailureCount = 0;

        while (!ct.IsCancellationRequested)
        {
            await using AsyncServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();
            IServiceProvider services = serviceScope.ServiceProvider;
            services.GetRequiredService<EnvironmentContextProvider>().Environment = _options.DataSourceEnvironment;

            IClientDataCacheService _clientDataCache = services.GetRequiredService<IClientDataCacheService>();
            IServerDataCacheService _serverDataCache = services.GetRequiredService<IServerDataCacheService>();
            ILocaleService _localeService = services.GetRequiredService<ILocaleService>();

            try
            {
                await _clientDataCache.RepopulateAsync(ct).ConfigureAwait(false);
                await _serverDataCache.RepopulateAsync(ct).ConfigureAwait(false);
                await _localeService.RepopulateAsync(ct).ConfigureAwait(false);
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
                new FireModeCollectionBuilder(),
                new FireModeToProjectileCollectionBuilder(),
                new ItemCollectionBuilder(),
                new ItemCategoryCollectionBuilder(),
                new WeaponCollectionBuilder(),
                new WorldCollectionBuilder()
            };

            foreach (ICollectionBuilder collectionBuilder in collectionBuilders)
            {
                try
                {
                    collectionBuilder.Build(_clientDataCache, _serverDataCache, _localeService, _collectionsContext);
                    _logger.LogInformation("Successfully ran the {CollectionBuilder}", collectionBuilder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run the {CollectionBuilder}", collectionBuilder);
                }
            }
            _collectionsContext.BuildCollectionInfos();

            await Task.Delay(TimeSpan.FromHours(1), ct).ConfigureAwait(false);
        }
    }
}