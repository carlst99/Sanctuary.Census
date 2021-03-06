using Sanctuary.Census.Abstractions.CollectionBuilders;
using Sanctuary.Census.Abstractions.Database;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.ClientData.ClientDataModels;
using Sanctuary.Census.Common.Objects.CommonModels;
using Sanctuary.Census.Exceptions;
using Sanctuary.Census.Models.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sanctuary.Census.CollectionBuilders;

/// <summary>
/// Builds the <see cref="Item"/> collection.
/// </summary>
public class ItemCollectionBuilder : ICollectionBuilder
{
    private readonly IClientDataCacheService _clientDataCache;
    private readonly ILocaleDataCacheService _localeDataCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCollectionBuilder"/> class.
    /// </summary>
    /// <param name="clientDataCache">The client data cache.</param>
    /// <param name="localeDataCache">The locale data cache.</param>
    public ItemCollectionBuilder
    (
        IClientDataCacheService clientDataCache,
        ILocaleDataCacheService localeDataCache
    )
    {
        _clientDataCache = clientDataCache;
        _localeDataCache = localeDataCache;
    }

    /// <inheritdoc />
    public async Task BuildAsync
    (
        IMongoContext dbContext,
        CancellationToken ct
    )
    {
        if (_clientDataCache.ClientItemDefinitions.Count == 0)
            throw new MissingCacheDataException(typeof(ClientItemDefinition));

        if (_clientDataCache.ItemProfiles.Count == 0)
            throw new MissingCacheDataException(typeof(ItemProfile));

        if (_clientDataCache.ImageSetMappings.Count == 0)
            throw new MissingCacheDataException(typeof(ImageSetMapping));

        Dictionary<uint, FactionDefinition> itemFactionMap = new();
        foreach (ItemProfile profile in _clientDataCache.ItemProfiles)
        {
            itemFactionMap.TryAdd(profile.ItemID, profile.FactionID);
            if (itemFactionMap[profile.ItemID] != profile.FactionID)
                itemFactionMap[profile.ItemID] = FactionDefinition.All;
        }

        Dictionary<uint, uint> imageSetToPrimaryImageMap = new();
        foreach (ImageSetMapping mapping in _clientDataCache.ImageSetMappings)
        {
            if (mapping.ImageType is not ImageSetType.Large)
                continue;

            imageSetToPrimaryImageMap[mapping.ImageSetID] = mapping.ImageID;
        }

        Dictionary<uint, Item> builtItems = new();
        foreach (ClientItemDefinition definition in _clientDataCache.ClientItemDefinitions)
        {
            _localeDataCache.TryGetLocaleString(definition.NameID, out LocaleString? name);
            _localeDataCache.TryGetLocaleString(definition.DescriptionID, out LocaleString? description);

            if (!itemFactionMap.TryGetValue(definition.ID, out FactionDefinition faction))
                faction = FactionDefinition.All;

            uint? imageID = null;
            string? imagePath = null;
            if (definition.ImageSetID > 0)
            {
                if (imageSetToPrimaryImageMap.ContainsKey((uint)definition.ImageSetID))
                {
                    imageID = imageSetToPrimaryImageMap[(uint)definition.ImageSetID];
                    imagePath = $"/files/ps2/images/static/{imageID}.png";
                }
            }

            Item built = new
            (
                definition.ID,
                definition.ItemType,
                definition.CategoryID,
                faction,
                name,
                description,
                definition.ActivatableAbilityID == 0 ? null : definition.ActivatableAbilityID,
                definition.PassiveAbilityID == 0 ? null : definition.PassiveAbilityID,
                definition.PassiveAbilitySetID == 0 ? null: definition.PassiveAbilitySetID,
                definition.SkillSetID == 0 ? null : definition.SkillSetID,
                definition.ImageSetID <= 0 ? null : (uint)definition.ImageSetID,
                imageID,
                imagePath,
                definition.HudImageSetID == 0 ? null : definition.HudImageSetID,
                definition.MaxStackSize,
                definition.FlagAccountScope
            );
            builtItems.TryAdd(built.ItemID, built);
        }

        await dbContext.UpsertItemsAsync(builtItems.Values, ct);
    }
}
