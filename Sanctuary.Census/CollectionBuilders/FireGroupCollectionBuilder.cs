﻿using Sanctuary.Census.Abstractions.CollectionBuilders;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.Common.Abstractions.Services;
using Sanctuary.Census.Exceptions;
using Sanctuary.Census.ServerData.Internal.Abstractions.Services;
using Sanctuary.Zone.Packets.ReferenceData;
using System.Collections.Generic;
using MFireGroup = Sanctuary.Census.Models.Collections.FireGroup;
using SFireGroup = Sanctuary.Zone.Packets.ReferenceData.FireGroup;

namespace Sanctuary.Census.CollectionBuilders;

/// <summary>
/// Builds the <see cref="MFireGroup"/> collection.
/// </summary>
public class FireGroupCollectionBuilder : ICollectionBuilder
{
    /// <inheritdoc />
    public void Build
    (
        IClientDataCacheService clientDataCache,
        IServerDataCacheService serverDataCache,
        ILocaleService localeService,
        CollectionsContext context
    )
    {
        if (serverDataCache.WeaponDefinitions is null)
            throw new MissingCacheDataException(typeof(WeaponDefinitions));

        Dictionary<uint, MFireGroup> builtFireGroups = new();
        foreach (SFireGroup fireGroup in serverDataCache.WeaponDefinitions.FireGroups)
        {
            MFireGroup built = new
            (
                fireGroup.FireGroupID,
                fireGroup.FireModes,
                fireGroup.ChamberDurationMs == 0 ? null : fireGroup.ChamberDurationMs,
                fireGroup.TransitionDurationMs == 0 ? null : fireGroup.TransitionDurationMs,
                fireGroup.SpoolUpTimeMs == 0 ? null : fireGroup.SpoolUpTimeMs,
                fireGroup.SpoolUpInitialRefireMs == 0 ? null : fireGroup.SpoolUpInitialRefireMs,
                (fireGroup.Flags & FireGroupFlags.CanChamberIronSights) != 0
            );
            builtFireGroups.TryAdd(built.FireGroupID, built);
        }

        context.FireGroups = builtFireGroups;
    }
}