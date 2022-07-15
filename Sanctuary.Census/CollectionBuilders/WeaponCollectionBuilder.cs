﻿using Sanctuary.Census.Abstractions.CollectionBuilders;
using Sanctuary.Census.ClientData.Abstractions.Services;
using Sanctuary.Census.Common.Abstractions.Services;
using Sanctuary.Census.Common.Objects.CommonModels;
using Sanctuary.Census.Exceptions;
using Sanctuary.Census.Models.Collections;
using Sanctuary.Census.ServerData.Internal.Abstractions.Services;
using Sanctuary.Zone.Packets.ReferenceData;
using System.Collections.Generic;
using System.Linq;

namespace Sanctuary.Census.CollectionBuilders;

/// <summary>
/// Builds the <see cref="Weapon"/> collection.
/// </summary>
public class WeaponCollectionBuilder : ICollectionBuilder
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

        Dictionary<uint, Weapon> builtWeapons = new();
        foreach (WeaponDefinition definition in serverDataCache.WeaponDefinitions.Definitions)
        {
            localeService.TryGetLocaleString(definition.RangeDescriptionID, out LocaleString? rangeDescription);

            string? animWieldTypeName = definition.AnimationWieldTypeName.Length == 0
                ? null
                : definition.AnimationWieldTypeName;

            Weapon.AmmoSlot[] ammoSlots = definition.AmmoSlots.Where(s => s.AmmoID != 0)
                .Select(s => new Weapon.AmmoSlot
                (
                    s.ClipSize,
                    s.Capacity,
                    s.ClipModelName.Length == 0 ? null : s.ClipModelName
                ))
                .ToArray();

            Weapon built = new
            (
                definition.WeaponID,
                definition.GroupID == 0 ? null : definition.GroupID,
                definition.EquipMs,
                definition.UnequipMs,
                definition.ToIronSightsMs,
                definition.FromIronSightsMs,
                definition.ToIronSightsAnimMs,
                definition.FromIronSightsAnimMs,
                definition.SprintRecoveryMs,
                definition.NextUseDelayMs,
                definition.TurnRateModifier,
                definition.MoveSpeedModifier,
                definition.HeatBleedoffRate == 0 ? null : definition.HeatBleedoffRate,
                definition.HeatOverheatPenaltyMs == 0 ? null : definition.HeatBleedoffRate,
                rangeDescription,
                definition.MeleeDetectWidth == 0 ? null : definition.MeleeDetectWidth,
                definition.MeleeDetectHeight == 0 ? null : definition.MeleeDetectHeight,
                animWieldTypeName,
                definition.MinPitch == 0 ? null : definition.MinPitch,
                definition.MaxPitch == 0 ? null : definition.MaxPitch,
                ammoSlots
            );
            builtWeapons.TryAdd(built.WeaponID, built);
        }

        context.Weapons = builtWeapons;
    }
}