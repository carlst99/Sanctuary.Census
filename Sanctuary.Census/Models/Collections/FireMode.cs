﻿using Sanctuary.Census.Common.Objects.CommonModels;

namespace Sanctuary.Census.Models.Collections;

#pragma warning disable CS1591
public record FireMode
#pragma warning restore CS1591
(
    uint FireModeID,
    byte FireModeTypeID,
    LocaleString Description,
    uint? AbilityID,
    uint? AmmoItemID,
    byte AmmoSlot,
    bool ArmorPenetration,
    bool Automatic,
    bool GriefImmune,
    bool LaserGuided,
    bool IronSights,
    bool SprintFire,
    bool SwayCanSteady,
    bool UseInWater,
    float CofOverride,
    float CofPelletSpread,
    float CofRange,
    float CofRecoil,
    float CofScalar,
    float CofScalarMoving,
    float? DamageHeadMultiplier,
    float? DamageLegsMultiplier,
    uint? DamageIndirectEffectID,
    byte FireAmmoPerShot,
    ushort FireAutoFireMs,
    byte FireBurstCount,
    ushort FireChargeUpMs,
    ushort FireCooldownDurationMs,
    ushort FireDelayMs,
    ushort FireDurationMs,
    ushort FireDetectRange,
    byte FirePelletsPerShot,
    ushort FireRefireMs,
    uint? HeatPerShot,
    ushort? HeatRecoveryDelayMs,
    uint? HeatThreshold,
    int MaxDamage,
    int MaxDamageRange,
    int MaxDamageInd,
    float MaxDamageIndRadius,
    int MinDamage,
    int MinDamageRange,
    int MinDamageInd,
    float MinDamageIndRadius,
    float MoveModifier,
    float TurnModifier,
    uint? PlayerStateGroupID,
    float? ProjectileSpeedOverride,
    float? RecoilAngleMax,
    float? RecoilAngleMin,
    float RecoilFirstShotModifier,
    float? RecoilHorizontalMax,
    float? RecoilHorizontalMaxIncrease,
    float? RecoilHorizontalMin,
    float? RecoilHorizontalMinIncrease,
    float? RecoilHorizontalTolerance,
    float? RecoilIncrease,
    float? RecoilIncreaseCrouched,
    float? RecoilMagnitudeMax,
    float? RecoilMagnitudeMin,
    float? RecoilMaxTotalMagnitude,
    float? RecoilRecoveryAcceleration,
    ushort? RecoilRecoveryDelayMs,
    float? RecoilRecoveryRate,
    byte? RecoilShotsAtMinMagnitude,
    ushort? ReloadAmmoFillMs,
    bool ReloadBlockAuto,
    ushort? ReloadChamberMs,
    bool ReloadContinuous,
    ushort? ReloadLoopStartMs,
    ushort? ReloadLoopEndMs,
    ushort ReloadTimeMs,
    int ShieldBypassPct,
    float? SwayAmplitudeX,
    float? SwayAmplitudeY,
    float? SwayPeriodX,
    float? SwayPeriodY,
    float ZoomDefault
);