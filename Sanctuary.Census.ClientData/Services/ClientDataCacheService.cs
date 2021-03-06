using CommunityToolkit.HighPerformance.Buffers;
using Mandible.Abstractions.Pack2;
using Mandible.Pack2;
using Mandible.Services;
using Mandible.Util;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.ClientData.ClientDataModels;
using Sanctuary.Census.ClientData.Util;
using Sanctuary.Census.Common.Abstractions.Services;
using Sanctuary.Census.Common.Objects;
using Sanctuary.Census.Common.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sanctuary.Census.ClientData.Services;

/// <inheritdoc />
public class ClientDataCacheService : IClientDataCacheService
{
    private readonly IManifestService _manifestService;
    private readonly EnvironmentContextProvider _environmentContextProvider;

    /// <inheritdoc />
    public DateTimeOffset LastPopulated { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ClientItemDatasheetData> ClientItemDatasheetDatas { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ClientItemDefinition> ClientItemDefinitions { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Currency> Currencies { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Experience> Experiences { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Faction> Factions { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<FireModeDisplayStat> FireModeDisplayStats { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ImageSetMapping> ImageSetMappings { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ItemProfile> ItemProfiles { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ResourceType> ResourceTypes { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Vehicle> Vehicles { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientDataCacheService"/> class.
    /// </summary>
    /// <param name="manifestService">The manifest service.</param>
    /// <param name="environmentContextProvider">The environment context provider.</param>
    public ClientDataCacheService
    (
        IManifestService manifestService,
        EnvironmentContextProvider environmentContextProvider
    )
    {
        _manifestService = manifestService;
        _environmentContextProvider = environmentContextProvider;
        Clear();
    }

    /// <inheritdoc />
    public async Task RepopulateAsync(CancellationToken ct = default)
    {
        ManifestFile dataPackManifest = await _manifestService.GetFileAsync
        (
            "data_x64_0.pack2",
            _environmentContextProvider.Environment,
            ct
        ).ConfigureAwait(false);

        await using Stream dataPackStrema = await _manifestService.GetFileDataAsync(dataPackManifest, ct)
            .ConfigureAwait(false);
        await using StreamDataReaderService sdrs = new(dataPackStrema, false);

        using Pack2Reader reader = new(sdrs);
        IReadOnlyList<Asset2Header> assetHeaders = await reader.ReadAssetHeadersAsync(ct).ConfigureAwait(false);

        ClientItemDatasheetDatas = await ExtractDatasheet<ClientItemDatasheetData>
        (
            "ClientItemDatasheetData.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        ClientItemDefinitions = await ExtractDatasheet<ClientItemDefinition>
        (
            "ClientItemDefinitions.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        Currencies = await ExtractDatasheet<Currency>
        (
            "Currency.txt",
            assetHeaders,
            reader,
            ct
        );

        Experiences = await ExtractDatasheet<Experience>
        (
            "Experience.txt",
            assetHeaders,
            reader,
            ct
        );

        Factions = await ExtractDatasheet<Faction>
        (
            "Factions.txt",
            assetHeaders,
            reader,
            ct
        );

        FireModeDisplayStats = await ExtractDatasheet<FireModeDisplayStat>
        (
            "FireModeDisplayStats.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        ImageSetMappings = await ExtractDatasheet<ImageSetMapping>
        (
            "ImageSetMappings.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        ItemProfiles = await ExtractDatasheet<ItemProfile>
        (
            "ItemProfiles.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        ResourceTypes = await ExtractDatasheet<ResourceType>
        (
            "ResourceTypes.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        Vehicles = await ExtractDatasheet<Vehicle>
        (
            "Vehicles.txt",
            assetHeaders,
            reader,
            ct
        ).ConfigureAwait(false);

        LastPopulated = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    [MemberNotNull(nameof(ClientItemDatasheetDatas))]
    [MemberNotNull(nameof(ClientItemDefinitions))]
    [MemberNotNull(nameof(Currencies))]
    [MemberNotNull(nameof(Experiences))]
    [MemberNotNull(nameof(Factions))]
    [MemberNotNull(nameof(FireModeDisplayStats))]
    [MemberNotNull(nameof(ImageSetMappings))]
    [MemberNotNull(nameof(ItemProfiles))]
    [MemberNotNull(nameof(ResourceTypes))]
    [MemberNotNull(nameof(Vehicles))]
    public void Clear()
    {
        LastPopulated = DateTimeOffset.MinValue;
        ClientItemDatasheetDatas = new List<ClientItemDatasheetData>();
        ClientItemDefinitions = new List<ClientItemDefinition>();
        Currencies = new List<Currency>();
        Experiences = new List<Experience>();
        Factions = new List<Faction>();
        FireModeDisplayStats = new List<FireModeDisplayStat>();
        ImageSetMappings = new List<ImageSetMapping>();
        ItemProfiles = new List<ItemProfile>();
        ResourceTypes = new List<ResourceType>();
        Vehicles = new List<Vehicle>();
    }

    private static async Task<List<TDataType>> ExtractDatasheet<TDataType>
    (
        string datasheetFileName,
        IEnumerable<Asset2Header> assetHeaders,
        IPack2Reader packReader,
        CancellationToken ct
    )
    {
        ulong nameHash = PackCrc64.Calculate(datasheetFileName);
        Asset2Header header = assetHeaders.First(ah => ah.NameHash == nameHash);

        using MemoryOwner<byte> data = await packReader.ReadAssetDataAsync(header, ct).ConfigureAwait(false);
        return DatasheetSerializer.Deserialize<TDataType>(data.Memory).ToList();
    }
}
